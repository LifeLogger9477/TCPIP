using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace appTcpipClient
{
	class NLogger
	{
		private static NLog.Logger logger;

		private static readonly Lazy<NLogger> lazy = new Lazy<NLogger>(() => new NLogger());

		public static NLogger Instance
		{
			get
			{
				return lazy.Value;
			}
		}

		public NLogger()
		{
			LoggingConfiguration config = new LoggingConfiguration();
			FileTarget fileTarget = new FileTarget();
			fileTarget.Encoding = Encoding.UTF8;
			fileTarget.FileName = String.Format("{0}/Log/{1}/{2}.log"
													, "${basedir}", @"${date:format=yyyyMM}", @"${date:format=yyyyMMdd}");
			fileTarget.Layout = String.Format("{0} / {1} / {2}"
													, "${level}", @"${date:format=yyyy-MM-dd HH\:mm\:ss.fff}", "${message}");
			fileTarget.ConcurrentWrites = true;
			fileTarget.ArchiveFileName = String.Format("{0}\\Log\\_backup\\{1}_{2}.log"
															, "${basedir}", @"log", @"${date:format=yyyyMMdd}");
			fileTarget.ArchiveAboveSize = 1024 * 1024 * 60;
			fileTarget.ArchiveEvery = FileArchivePeriod.Day;
			fileTarget.MaxArchiveFiles = 30;
			config.AddTarget("file", fileTarget);

			LoggingRule rule = new LoggingRule("*", LogLevel.Debug, fileTarget);
			config.LoggingRules.Add(rule);
			LogManager.Configuration = config;
			logger = LogManager.GetLogger("ModbusHandler");
		}

		public void Debug( string msg )
		{
			logger.Debug(msg);
		}

		public void Debug( string msg, params object[] paramData )
		{
			logger.Debug(String.Format(msg, paramData));
		}

		public void Error( string msg )
		{
			logger.Error(msg);
		}

		public void Error( Exception ex )
		{
			logger.Error(ex);
		}

		public void Error( string msg, params object[] paramData )
		{
			logger.Error(String.Format(msg, paramData));
		}

		public void Fatal( string msg )
		{
			logger.Fatal(msg);
		}

		public void Fatal( string msg, params object[] paramData )
		{
			logger.Fatal(msg, paramData);
		}


		public void Info( string msg )
		{
			logger.Info(msg);
		}

		public void Info( string msg, params object[] paramData )
		{
			logger.Info(msg, paramData);
		}

		public void Warn( string msg )
		{
			logger.Warn(msg);
		}

		public void Warn( string msg, params object[] paramData )
		{
			logger.Warn(msg, paramData);
		}
	}
}
