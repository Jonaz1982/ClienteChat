using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
//using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



using ClienteChat.srServicioChat;
using System.Windows.Threading;
using System.ServiceModel;

using System.Reflection;
using Microsoft.Win32;
using System.IO;

namespace ClienteChat
{

    public partial class Principal : Window, IChatCallback
    {

        //Lista que almacena los usuarios y el item de lista respectivo
        Dictionary<ListBoxItem, srServicioChat.Cliente> ListaUsuarios = new Dictionary<ListBoxItem, Cliente>();
        string rcvFilesPath = @"C:/WCF_Received_Files/";

       
        //Método constructor
        public Principal()
        {
            InitializeComponent();
                   
            //Evento para actualizar interface de conversion
            AdministrarServicioChat.ActualizarInterfaceConversacion += new EventHandler(ActualizarInterfaceConversacion);

            //Evento para actualizar estado de conversion
            AdministrarServicioChat.ActualizoEstadoConversacion += new EventHandler(ActualizarEstadoConversacion);

            //Asignar distribuidor de hilos
            AdministrarServicioChat.Distribuidor = this.Dispatcher;

            MostrarConversacion(false);
            MostrarIngreso(true);

            lstUsuarios.SelectionChanged += new SelectionChangedEventHandler(lstUsuarios_SelectionChanged);
            txtMensaje.KeyDown += new KeyEventHandler(txtMensaje_KeyDown);
            txtMensaje.KeyUp += new KeyEventHandler(txtMensaje_KeyUp);
        }
   
        #region Metodos Interface de Usuario
        
        //Enfocar un Mensaje en la lista de mensajes cuando viene uno nuevo
        private ScrollViewer EnfocarMensaje(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ScrollViewer)
                {
                    return (ScrollViewer)child;
                }
                else
                {
                    ScrollViewer childOfChild = EnfocarMensaje(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        //Metodo para mostrar interface de logueo
        private void MostrarIngreso(Boolean Mostrar)
        {
            if (Mostrar)
            {
                btnConectar.Visibility = Visibility.Visible;
                lblIP.Visibility = Visibility.Visible;
                lblUsuario.Visibility = Visibility.Visible;
                txtIP.Visibility = Visibility.Visible;
                txtUsuario.Visibility = Visibility.Visible;
            }
            else
            {
                btnConectar.Visibility = Visibility.Collapsed;
                lblIP.Visibility = Visibility.Collapsed;
                lblUsuario.Visibility = Visibility.Collapsed;
                txtIP.Visibility = Visibility.Collapsed;
                txtUsuario.Visibility = Visibility.Collapsed;
            }
        }

        //Metodo para mostrar interface de conversación
        private void MostrarConversacion(Boolean Mostrar)
        {
            if (Mostrar)
            {
                btnDesconectar.Visibility = Visibility.Visible;
                btnEnviar.Visibility = Visibility.Visible;
                imgUsuarioActual.Visibility = Visibility.Visible;
                lblUsuarioActual.Visibility = Visibility.Visible;
                lstConversacion.Visibility = Visibility.Visible;
                lstUsuarios.Visibility = Visibility.Visible;
                txtMensaje.Visibility = Visibility.Visible;
            }
            else
            {
                btnDesconectar.Visibility = Visibility.Collapsed;
                btnEnviar.Visibility = Visibility.Collapsed;
                imgUsuarioActual.Visibility = Visibility.Collapsed;
                lblUsuarioActual.Visibility = Visibility.Collapsed;
                lstConversacion.Visibility = Visibility.Collapsed;
                lstUsuarios.Visibility = Visibility.Collapsed;
                txtMensaje.Visibility = Visibility.Collapsed;
            }
        }

        public void ActualizarInterfaceConversacion(object sender, EventArgs e)
        {
            switch (AdministrarServicioChat.EstadoComunicacion)
            {
                case CommunicationState.Closed:
                    lstConversacion.Items.Clear();
                    lstUsuarios.Items.Clear();
                    MostrarConversacion(false);
                    MostrarIngreso(true);
                    btnConectar.IsEnabled = true;
                    break;
                case CommunicationState.Faulted:
                    lstConversacion.Items.Clear();
                    lstUsuarios.Items.Clear();
                    MostrarConversacion(false);
                    MostrarIngreso(true);
                    btnConectar.IsEnabled = true;
                    break;
                case CommunicationState.Opened:
                    MostrarIngreso(false);
                    MostrarConversacion(true);
                    lblUsuarioActual.Content = AdministrarServicioChat.UsuarioActual.Usuario;
                    break;
                case CommunicationState.Opening:
                    break;
                default:
                    break;
            }
            lblEstadoConversacion.Text = AdministrarServicioChat.EstadoConversacion;
        }

        public void ActualizarEstadoConversacion(object sender, EventArgs e)
        {
            lblEstadoConversacion.Text = AdministrarServicioChat.EstadoConversacion;
        }

        #endregion
        
        #region Eventos Interface de Usuario

        void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            DirectoryInfo dir = new DirectoryInfo(rcvFilesPath);
            dir.Create();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            AdministrarServicioChat.Cerrar();
        }

        private void btnConectar_Click(object sender, RoutedEventArgs e)
        {
            btnConectar.IsEnabled = false;
            lblEstadoConversacion.Text = "Conectando...";

            if (!AdministrarServicioChat.Conectar(txtUsuario.Text, txtIP.Text, this))
                btnConectar.IsEnabled = true;
            lblEstadoConversacion.Text = AdministrarServicioChat.EstadoConversacion;
        }

        private void btnEnviar_Click(object sender, RoutedEventArgs e)
        {
            AdministrarServicioChat.Enviar(txtMensaje.Text);
            txtMensaje.Text = "";
            txtMensaje.Focus();
        }

        private void btnDesconectar_Click(object sender, RoutedEventArgs e)
        {
            AdministrarServicioChat.Desconectar();
        }

        void txtMensaje_KeyUp(object sender, KeyEventArgs e)
        {
            AdministrarServicioChat.DesactivarEstaEscribiendo(txtMensaje.Text);
        }

        void txtMensaje_KeyDown(object sender, KeyEventArgs e)
        {
            AdministrarServicioChat.ActivarEstaEscribiendo(txtMensaje.Text, e.Key);
        }

        private void btnEnviarArchivo_Click(object sender, RoutedEventArgs e)
        {
           try
           {
            
               if (lstUsuarios.SelectedIndex >= 0)
               {
                   OpenFileDialog ofd = new OpenFileDialog();
                   ofd.Multiselect = false;

                   if (ofd.ShowDialog() != DialogResult.HasValue)
                   {
                       Stream s = ofd.OpenFile();
                     
                           byte[] bArchivo = new byte[(int)s.Length];
                           int n = s.Read(bArchivo, 0, bArchivo.Length);

                           if (n > 0)
                           {
                               AdministrarServicioChat.EnviarArchivo(ofd.SafeFileName, bArchivo);
                           }
                   }
               }
               else
               {
                   txtMensaje.Text = "Debe seleccionar un usuario";
               }
           }
           catch (Exception ex)
           {
               txtMensaje.Text = ex.Message.ToString();               
           }

        }

        //void proxy_EnviarArchivoCompleted(object sender, EnviarArchivoCompletedEventArgs e)
        //{
            
        //    ListBoxItem item = AdministrarServicioChat.ActualizarConversacion( lstUsuarios.Name + " Enviando archivo....");
        //    lstConversacion.Items.Add(item);
            
        //}
        void lstUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //conecta al usuario
            //Definir un usuario receptor para enviarle archivos
            ListBoxItem item = lstUsuarios.SelectedItem as ListBoxItem;
            if (item != null)
            {
                AdministrarServicioChat.UsuarioReceptor = ListaUsuarios[item];
            }
        }

        #endregion
        

        #region Miembros de IChatRespuesta

        public void RefrescarClientes(List<Cliente> clientes)
        {
            lstUsuarios.Items.Clear();
            ListaUsuarios.Clear();
            foreach (srServicioChat.Cliente c in clientes)
            {
                ListBoxItem item =AdministrarServicioChat.ActualizarConversacion(c.Foto, c.Usuario);
                lstUsuarios.Items.Add(item);
                ListaUsuarios.Add(item, c);
            }
        }

        public void Recibir(Mensaje Mensaje)
        {
            foreach (srServicioChat.Cliente c in this.ListaUsuarios.Values)
            {
                if (c.Usuario == Mensaje.Remitente)
                {
                    ListBoxItem item = AdministrarServicioChat.ActualizarConversacion(c.Foto, Mensaje.Remitente + " : " + Mensaje.Contenido);
                    lstConversacion.Items.Add(item);
                }
            }
            ScrollViewer sv = EnfocarMensaje(lstConversacion);
            sv.LineDown();
        }

        public void RecibirArchivo(FileMessage fileMsg)
        {
                FileStream fileStrm = new FileStream(rcvFilesPath +
                           fileMsg.NombreArchivo, FileMode.Create, FileAccess.ReadWrite);
                fileStrm.Write(fileMsg.Datos, 0, fileMsg.Datos.Length);           

                foreach (srServicioChat.Cliente c in this.ListaUsuarios.Values)
                {
                    ListBoxItem item = AdministrarServicioChat.ActualizarConversacion(c.Foto, "Archivo recibido, " + fileMsg.NombreArchivo);
                    lstConversacion.Items.Add(item);
                }
        }

        public void RecibirMensaje(Mensaje Mensaje)
        {
            foreach (srServicioChat.Cliente c in this.ListaUsuarios.Values)
            {
                if (c.Usuario == Mensaje.Remitente)
                {
                    ListBoxItem item = AdministrarServicioChat.ActualizarConversacion(c.Foto, Mensaje.Remitente + " : " + Mensaje.Contenido);
                    lstConversacion.Items.Add(item);
                }
            }
            ScrollViewer sv = EnfocarMensaje(lstConversacion);
            sv.LineDown();
        }

        public void EstaEscribiendoRespuesta(Cliente cliente)
        {
           if (cliente == null)
            {
                lblEstadoEscribiendo.Text = "";
            }
            else
            {
                lblEstadoEscribiendo.Text = cliente.Usuario + " está escribiendo un mensaje...";
            }
        }

        public void Unirse(Cliente cliente)
        {
            ListBoxItem item = AdministrarServicioChat.ActualizarConversacion(cliente.Foto,
                "|||||||||||||||||||| " + cliente.Usuario + " se ha unido al chat ||||||||||||||||||||");
            lstConversacion.Items.Add(item);
            ScrollViewer sv = EnfocarMensaje(lstConversacion);
            //sv.LineDown();
        }

        public void Dejar(Cliente cliente)
        {
            ListBoxItem item = AdministrarServicioChat.ActualizarConversacion(cliente.Foto,
                "|||||||||||||||||||| " + cliente.Usuario + " ha dejado el chat ||||||||||||||||||||");
            lstConversacion.Items.Add(item);
            ScrollViewer sv = EnfocarMensaje(lstConversacion);
            sv.LineDown();
        }
        
        private void btnAbrirArchivo_Click(object sender, RoutedEventArgs e)
        {
            //Open WCF_Received_Files folder in windows explorer
            System.Diagnostics.Process.Start(rcvFilesPath);
        }

        #endregion

        #region Async

        public IAsyncResult BeginDejar(Cliente cliente, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndDejar(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginUnirse(Cliente cliente, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndUnirse(IAsyncResult result)
        {
            throw new NotImplementedException();
        }


        public IAsyncResult BeginEstaEscribiendoRespuesta(Cliente cliente, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndEstaEscribiendoRespuesta(IAsyncResult result)
        {
            throw new NotImplementedException();
        }    

        public IAsyncResult BeginRecibir(Mensaje Mensaje, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndRecibir(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginRefrescarClientes(List<Cliente> clientes, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndRefrescarClientes(IAsyncResult result)
        {
            throw new NotImplementedException();
        }
               
        public void Respuesta(Mensaje msg)
        {
            throw new NotImplementedException();
        }
        
        public IAsyncResult BeginRespuesta(Mensaje msg, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndRespuesta(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginRecibirArchivo(FileMessage fileMsg, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }

        public void EndRecibirArchivo(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
