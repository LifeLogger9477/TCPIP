using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SimpleTcp;

namespace appSimpleTcpipServer
{
	public partial class frmMain : MetroFramework.Forms.MetroForm
	{
		enum ServerState
		{
			Serveris_Stop = 0,
			Serveris_Run
		}

		private static readonly string[] _ServerStateString =
		{
			@"Stop Server",
			@"Run Server"
		};

		struct _ServerParams
		{
			public static string sIP;
			public static string sPort;
		}

		public static long count = 0;

		SimpleTcpServer server;

		public frmMain()
		{
			string msg = string.Empty;
			msg = @"appSimpleTcpipServer Start.";
			NLogger.Instance.Debug(msg);

			InitializeComponent();

			if( !openConfigurations() )
			{
				msg = "Error to read configuration info.";
				NLogger.Instance.Fatal(msg);
				MessageBox.Show(msg, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				Application.Exit();
			}
		}

		private void frmMain_Load( object sender, EventArgs e )
		{
			InputMessage(@"Start Server...");

			string serverinfo = _ServerParams.sIP + @":" + _ServerParams.sPort;
			server = new SimpleTcpServer(serverinfo);
			server.Events.ClientConnected += Events_ClientConnected;
			server.Events.ClientDisconnected += Events_ClientDisconnected;
			server.Events.DataReceived += Events_DataReceived;

			server.Start();
		}

		private void Events_ClientConnected( object sender, ClientConnectedEventArgs e )
		{
			string msg = string.Empty;
			this.Invoke((MethodInvoker) delegate
			{
				msg = $"{e.IpPort} connected.";
				NLogger.Instance.Debug(msg);
				InputMessage(msg);
			});
		}

		private void Events_ClientDisconnected( object sender, ClientDisconnectedEventArgs e )
		{
			string msg = string.Empty;
			this.Invoke((MethodInvoker) delegate
			{
				msg = $"{e.IpPort} disconnected.";
				NLogger.Instance.Debug(msg);
				InputMessage(msg);
			});
		}

		private void Events_DataReceived( object sender, DataReceivedEventArgs e )
		{
			string msg = string.Empty;
			this.Invoke((MethodInvoker) delegate
			{
				msg = $"{e.IpPort} : {Encoding.UTF8.GetString(e.Data)}";
				NLogger.Instance.Debug(msg);
				InputMessage(msg);
			});

			if (Encoding.UTF8.GetString(e.Data) == @"READ?")
			{
				if (server.IsListening)
				{
					count++;
					msg = String.Format(@"Server Send Data - {0}", count);
					server.Send(e.IpPort, msg);
					this.Invoke((MethodInvoker) delegate
					{
						msg = String.Format(@"Recieved Server Data - {0}", count);
						NLogger.Instance.Debug(msg);
						InputMessage(msg);
					});
				}
			}
		}

		private bool openConfigurations()
		{
			string msg = string.Empty;

			if (!openConfigurations_ServerParams())
			{
				msg = "Error to read configuration info for Server Parameter.";
				NLogger.Instance.Fatal(msg);

				return false;
			}

			return true;
		}

		private bool openConfigurations_ServerParams()
		{
			var section = (NameValueCollection) ConfigurationManager.GetSection(@"ServerParams");
			string svalue = String.Empty;
			string msg = String.Empty;

			if( section == null ) {
				msg = @"Section 'Server Params' is not configuration file.";
				NLogger.Instance.Fatal(msg);
			}
			else
			{
				svalue = section[@"ServerIP"];
				_ServerParams.sIP = svalue;

				svalue = section[@"ServerPort"];
				_ServerParams.sPort = svalue;
			}

			return true;
		}

		private void InputMessage( string msg )
		{
			string sDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string sMessage = sDateTime + " : " + msg;

			lstMessage.Items.Add(sMessage);
			lstMessage.SelectedIndex = lstMessage.Items.Count - 1;

			if( lstMessage.Items.Count > 1000 )
			{
				lstMessage.Items.Clear();
			}
		}
		
	}
}
