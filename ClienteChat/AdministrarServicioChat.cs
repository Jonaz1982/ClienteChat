using System;
using System.Windows.Controls;

using ClienteChat.srServicioChat;
using System.ServiceModel;
using System.Windows.Threading;

using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows;
using ClienteChat;

public class AdministrarServicioChat
{
    //Referencias al Proxy y Contratos
    private static ChatClient proxy = null;
    private static Cliente usuarioReceptor = null;
    private static Cliente usuarioActual = null;
    
    private static String estadoConversacion;

    private static Dispatcher distribuidor;

    private static CommunicationState estadoComunicacion;

    //Se requiere un nuevo hilo para llamar el evento
    //en caso de que la comunicación falle
    private delegate void FaultedInvoker();

    //Evento para actualizar la interface de conversacion cada vez que se gestiona el proxy
    public static event EventHandler ActualizarInterfaceConversacion;

    //Evento para actualizar el mensaje de Estado de la Conversación
    public static event EventHandler ActualizoEstadoConversacion;

    #region Propiedades

    public static String EstadoConversacion
    {
        get
        {
            return estadoConversacion;
        }
    }

    public static Dispatcher Distribuidor
    {
        set
        {
            distribuidor = value;
        }
    }

    public static Cliente UsuarioActual
    {
        get
        {
            return usuarioActual;
        }
    }

    public static Cliente UsuarioReceptor
    {
        set
        {
            usuarioReceptor = value;
        }
    }

    public static CommunicationState EstadoComunicacion
    {
        get
        {
            return estadoComunicacion;
        }
    }

    #endregion

    //Metodo que se llama para poder saber cuando se pierde una conexión y en otras circustancias
    public static void GestionarProxy()
    {
        if (proxy != null)
        {
            estadoComunicacion = proxy.State;
            switch (proxy.State)
            {
                case CommunicationState.Closed:
                    proxy = null;
                    estadoConversacion = "Desconectado";
                    break;
                case CommunicationState.Closing:
                    estadoConversacion = "Cerrando...";
                    break;
                case CommunicationState.Created:
                    estadoConversacion = "Creada...";
                    break;
                case CommunicationState.Faulted:
                    proxy.Abort();
                    proxy = null;
                    estadoConversacion = "Fallida";
                    break;
                case CommunicationState.Opened:
                    estadoConversacion = "Conectado";
                    break;
                case CommunicationState.Opening:
                    estadoConversacion = "Abriendo...";
                    break;
                default:
                    break;
            }
            ActualizarInterfaceConversacion(null, new EventArgs());
        }

    }//Gestionar Proxy



    //Metodo que establece la conexion con el servicio
    public static Boolean Conectar(String Usuario,
                                     String IP,
                                     Object Contexto)
    {
        if (proxy == null)
        {
            try
            {
                usuarioActual = new Cliente();
                usuarioActual.Usuario = Usuario;
                InstanceContext context = new InstanceContext(Contexto);
                proxy = new ChatClient(context);

                //Definir la direccion IP del servicio
                string servicePath = proxy.Endpoint.ListenUri.AbsolutePath;
                string serviceListenPort = proxy.Endpoint.Address.Uri.Port.ToString();

                proxy.Endpoint.Address = new EndpointAddress("net.tcp://" + IP + ":" + serviceListenPort + servicePath);


                proxy.Open();

                proxy.InnerDuplexChannel.Faulted += new EventHandler(InnerDuplexChannel_Faulted);
                proxy.InnerDuplexChannel.Opened += new EventHandler(InnerDuplexChannel_Opened);
                proxy.InnerDuplexChannel.Closed += new EventHandler(InnerDuplexChannel_Closed);
                proxy.ConectarAsync(usuarioActual);
                proxy.ConectarCompleted += new EventHandler<ConectarCompletedEventArgs>(proxy_ConexionCompletada);
                return true;
            }
            catch (Exception ex)
            {
                estadoConversacion = "Fuera de Línea: " + ex.Message.ToString();
                ActualizoEstadoConversacion(null, new EventArgs());
                return false;
            }
        }
        else
        {
            AdministrarServicioChat.GestionarProxy();
            return true;
        }
    }//Conectar

    public static void Enviar(String Mensaje)
    {
        if (proxy != null && Mensaje != "")
        {
            if (proxy.State == CommunicationState.Faulted)
            {
                AdministrarServicioChat.GestionarProxy(); ;
            }
            else
            {
                //Crear el mensaje e iniciar propiedades
                Mensaje msg = new Mensaje();
                msg.Remitente = usuarioActual.Usuario;
                msg.Contenido = Mensaje;

                proxy.EnviarAsync(msg);
                //Indicar a los usuarios que el usuario actual esta escribiendo
                proxy.EstaEscribiendoAsync(null);
            }
        }
    }//Enviar


    public static void EnviarArchivo(String NombreArchivo, byte[] Contenido)
    {
        if (proxy != null && NombreArchivo != "" && Contenido != null)
        {
            if (proxy.State == CommunicationState.Faulted)
            {
                AdministrarServicioChat.GestionarProxy(); ;
            }
            else
            {
                FileMessage fMsg = new FileMessage();
                fMsg.NombreArchivo = NombreArchivo;
                fMsg.Datos = Contenido;
                proxy.EnviarArchivoAsync(fMsg);
          }
        }
    }
    
    public static void Cerrar()
    {
        if (proxy != null)
        {
            if (proxy.State == CommunicationState.Opened)
            {
                proxy.Desconectar(usuarioActual);
                //No ejecutar proxy.Close() porque  isTerminating = true en Desconectar()
                //lo cual llamaria a  GestionarProxy()
            }
            else
            {
                GestionarProxy(); ;
            }
        }
    }//Cerrar

    public static void Desconectar()
    {
        if (proxy != null)
        {
            if (proxy.State == CommunicationState.Faulted)
            {
                AdministrarServicioChat.GestionarProxy(); ;
            }
            else
            {
                proxy.DesconectarAsync(usuarioActual);
            }
        }
    }//Desconectar

    public static void ActivarEstaEscribiendo(String Mensaje, Key tecla)
    {
        if (proxy != null)
        {
            if (proxy.State == CommunicationState.Faulted)
            {
                AdministrarServicioChat.GestionarProxy(); ;
            }
            else
            {
                if (tecla == Key.Enter)
                {
                    Enviar(Mensaje);
                }
                else if (Mensaje.Length < 1)
                {
                    proxy.EstaEscribiendoAsync(usuarioActual);
                }
            }
        }
    }//ActivarEstaEscribiendo

    public static void DesactivarEstaEscribiendo(String Mensaje)
    {
        if (proxy != null)
        {
            if (proxy.State == CommunicationState.Faulted)
            {
                AdministrarServicioChat.GestionarProxy(); ;
            }
            else
            {
                if (Mensaje.Length < 1)
                {
                    proxy.EstaEscribiendoAsync(null);
                }
            }
        }
    }//DesactivarEstaEscribiendo

    #region Eventos

    //El objeto de comunicación activa un evento por cada transición de estado
    public static void InnerDuplexChannel_Closed(object sender, EventArgs e)
    {
        if (!distribuidor.CheckAccess())
        {
            distribuidor.BeginInvoke(DispatcherPriority.Normal, new FaultedInvoker(AdministrarServicioChat.GestionarProxy));
            return;
        }
        AdministrarServicioChat.GestionarProxy();
    }

    public static void InnerDuplexChannel_Opened(object sender, EventArgs e)
    {
        if (!distribuidor.CheckAccess())
        {
            distribuidor.BeginInvoke(DispatcherPriority.Normal, new FaultedInvoker(AdministrarServicioChat.GestionarProxy));
            return;
        }
        AdministrarServicioChat.GestionarProxy();
    }

    public static void InnerDuplexChannel_Faulted(object sender, EventArgs e)
    {
        if (!distribuidor.CheckAccess())
        {
            distribuidor.BeginInvoke(DispatcherPriority.Normal, new FaultedInvoker(AdministrarServicioChat.GestionarProxy));
            return;
        }
        AdministrarServicioChat.GestionarProxy();
    }

    public static void proxy_ConexionCompletada(object sender, ConectarCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            estadoConversacion = e.Error.Message.ToString();
            ActualizoEstadoConversacion(null, new EventArgs());
            //lblEstadoIngreso.Foreground = new SolidColorBrush(Colors.Red);
            //btnConectar.IsEnabled = true;
        }
        else
        {
            if (e.Result)
                GestionarProxy();
            else
            {
                estadoConversacion = "Usuario encontrado";
                ActualizoEstadoConversacion(null, new EventArgs());
                //btnConectar.IsEnabled = true;
            }
        }
    }

    #endregion

    //Metodo invocado cada vez que llega un mensaje desde el servivio,
    // Un cliente se une o abandona
    //Se agrega un elemento a la Conversación

    public static ListBoxItem ActualizarConversacion(byte[] foto, string text)
    {
        ListBoxItem item = new ListBoxItem();

        Image img = null;
        if (foto != null)
        {
            MemoryStream ms = new MemoryStream(foto);
            img = new Image();
            BitmapImage bi = new BitmapImage();
            bi.StreamSource = ms;

            img.Source = bi;

            img.Height = 70;
            img.Width = 60;
        }

        item.Content = img;

        TextBlock txtblock = new TextBlock();
        txtblock.Text = text;
        txtblock.VerticalAlignment = VerticalAlignment.Center;

        StackPanel panel = new StackPanel();
        panel.Orientation = Orientation.Horizontal;
        panel.Children.Add(item);
        panel.Children.Add(txtblock);

        ListBoxItem ItemCompleto = new ListBoxItem();
        ItemCompleto.Content = panel;

        return ItemCompleto;
    }

}

