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

using System.Net.Sockets;
using System.Threading;

namespace appTcpipClient
{
	public partial class frmMain : MetroFramework.Forms.MetroForm
	{
		//	background worker
		private BackgroundWorker bgworker = new BackgroundWorker();

		//	clientSocket
		TcpClient _clinetSocket;
		NetworkStream _serverStream;
		private Thread _messageThread;

		struct ServerInfo
		{
			public static string serverIP;
			public static int serverPort;
		}

		struct GlobalParameters
		{
			public static bool autoRun;
			public static bool runMonitoring;
		}

		public frmMain()
		{
			string msg = string.Empty;
			msg = @"appTcpipClient Start.";
			NLogger.Instance.Debug(msg);

			InitializeComponent();

			if( !openConfigurations() )
			{
				msg = "Error to read configuration info.";
				NLogger.Instance.Fatal(msg);
				MessageBox.Show(msg, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				Application.Exit();
			}

			GlobalParameters.runMonitoring = false;

			bgworker.WorkerReportsProgress = true;				//	진도 보고
			bgworker.WorkerSupportsCancellation = true;			//	취소 가능

			bgworker.DoWork += new DoWorkEventHandler(bgworker_doWork);
			bgworker.ProgressChanged += new ProgressChangedEventHandler(bgworker_ProgressChanged);
			bgworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgworker_runCompleted);

			if( !Connect_ClientSocket(out msg) ) {
				NLogger.Instance.Fatal(msg);
				MessageBox.Show(msg, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				Application.Exit();
			}

/*//
			if( GlobalParameters.autoRun )
			{
				msg = @"autoRun is true.";
				NLogger.Instance.Debug(msg);

				btnRun.PerformClick();
			}
//*/
		}

		private bool Connect_ClientSocket(out string msg)
		{
			try
			{
				_clinetSocket = new TcpClient();

				_serverStream = default(NetworkStream);

				if (!_clinetSocket.Connected)
				{
					_clinetSocket.Connect(ServerInfo.serverIP, ServerInfo.serverPort);
				}

				if (!_clinetSocket.Connected)
				{
					msg = String.Format(@"Server Connection is failed. IP:{0}, Port:{1}", ServerInfo.serverIP,
						ServerInfo.serverPort);
					NLogger.Instance.Error(msg);

					return false;
				}

				msg = String.Format(@"Server Connection is success. IP:{0}, Port:{1}", ServerInfo.serverIP,
					ServerInfo.serverPort);
				NLogger.Instance.Debug(msg);

				_messageThread = new Thread(getServerMessage);
				_messageThread.Start();
			}
			catch (SocketException se)
			{
				msg = se.Message;
				NLogger.Instance.Error(msg);

				return false;
			}
			catch (Exception ex)
			{
				msg = ex.Message;
				NLogger.Instance.Error(msg);

				return false;
			}

			return true;
		}

		private void getServerMessage()
		{
			string msg = string.Empty;
			string sreaddata = string.Empty;

			while( true ) {
				try
				{
					_serverStream = _clinetSocket.GetStream();
					var buffsize = _clinetSocket.ReceiveBufferSize;
					byte[] instream = new byte[buffsize];

					_serverStream.Read(instream, 0, buffsize);

					sreaddata = System.Text.Encoding.ASCII.GetString(instream).TrimEnd('\0');

					if( this.InvokeRequired ) {
						msg = String.Format("Read data : {0}", sreaddata);
						this.BeginInvoke(new MethodInvoker(() => InputMessage(msg)));
						this.BeginInvoke(new MethodInvoker(() => NLogger.Instance.Debug(msg)));
					}
				}
				catch (Exception ex)
				{
					NLogger.Instance.Error(ex.Message);
					break;
				}
			}
		}

		private void bgworker_doWork(object sender, DoWorkEventArgs e)
		{

			DateTime dtCur = DateTime.Now, dtNext;
			dtNext = dtCur;
			while (true)
			{
				dtCur = DateTime.Now;
				if (dtCur >= dtNext)
				{
					// 외부에서 작업을 취소하였는가?
					if (bgworker.CancellationPending)
					{
						e.Cancel = true;  // 작업 취소
						break;
					}
					// 현재 Data 읽기
					ReadCurrentServerData(e);

					dtNext = dtCur.AddSeconds(5); // 10초 간격
				}
				System.Threading.Thread.Sleep(10);
			}
		}

		private bool ReadCurrentServerData(DoWorkEventArgs e)
		{
			string sMESSAGE = string.Empty;
			try
			{
				if( _clinetSocket == null )
				{
					sMESSAGE = @"client socket is null.";
					NLogger.Instance.Error(sMESSAGE);
					return false;
				}

				if (_clinetSocket.Connected)
				{
					// Read
					byte[] outStream = Encoding.ASCII.GetBytes(@"READ?");
					_serverStream.Write(outStream, 0, outStream.Length);
					_serverStream.Flush();
				}
			}
			catch (Exception ex)
			{
				sMESSAGE = @"ReadCurrentServerData - " + ex.Message;
				NLogger.Instance.Error(sMESSAGE);
			}

			return true;
		}

		private void bgworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
            
		}

		private void bgworker_runCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			string sMESSAGE = "completed background worker.";
			NLogger.Instance.Debug(sMESSAGE);

			// 종료 후 Text 변경
			btnRun.Text = @"Run";
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

			if (!openConfigurations_User())
			{
				msg = "Error to read configuration info for User.";
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
				ServerInfo.serverIP = svalue;

				svalue = section[@"ServerPort"];
				ServerInfo.serverPort = Int32.Parse(svalue);
			}

			return true;
		}

		private bool openConfigurations_User()
		{
			var section = (NameValueCollection) ConfigurationManager.GetSection(@"appSettings");
			string msg = String.Empty;
			string svalue = string.Empty;

			if( section == null ) {
				msg = @"Section 'User config' is not configuration file.";
				NLogger.Instance.Fatal(msg);
			}
			else
			{
				svalue = section[@"AutoStart"];
				int nread = Int32.Parse(svalue);
				GlobalParameters.autoRun = Convert.ToBoolean(nread);
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

		private void btnRun_Click( object sender, EventArgs e )
		{
			GlobalParameters.runMonitoring = !GlobalParameters.runMonitoring;
			if (GlobalParameters.runMonitoring)
			{
				// 여기서 background 작업 시작
				if (bgworker != null)
				{
					bgworker.RunWorkerAsync();
				}
				btnRun.Text = @"Stop";

				NLogger.Instance.Debug(@"Start Monitoring..");
			}
			else
			{
				if (bgworker != null)
				{
					bgworker.CancelAsync();
				}

				NLogger.Instance.Debug(@"Stop Monitoring..");
			}
		}

		private void frmMain_FormClosing( object sender, FormClosingEventArgs e )
		{
			if( _clinetSocket != null )
			{
				_clinetSocket.Dispose();
			}

			if( bgworker != null )
			{
				bgworker.CancelAsync();
			}

			NLogger.Instance.Debug(@"Close Form.");
		}
	}
}
