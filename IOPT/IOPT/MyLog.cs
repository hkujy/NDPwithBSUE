using System;
using System.Text;
using System.Diagnostics;
using System.IO;
namespace IOPT
{
    // copy and modified based on http://www.infocool.net/kb/Other/201611/210142.html
    interface ILoger
    {
        void Warn(object msg);
        void Info(object msg);
        void Debug(object msg);
        void Error(object msg);
    }
    public class LogerTraceListener : TraceListener
    {
        /// <summary>
        /// FileName
        /// </summary>
        private string m_fileName;

        /// <summary>
        /// Constructor
        /// </summary>
        public LogerTraceListener()
        {
            this.m_fileName = MyFileNames.LogFolder +
                string.Format("Log-{0}.log", DateTime.Now.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(string message)
        {
            message = Format(message, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(object obj)
        {
            string message = Format(obj, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(object obj)
        {
            string message = Format(obj, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(string message)
        {
            message = Format(message, "");
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(object obj, string category)
        {
            string message = Format(obj, category);
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// WriteLine
        /// </summary>
        public override void WriteLine(string message, string category)
        {
            message = Format(message, category);
            File.AppendAllText(m_fileName, message);
        }

        /// <summary>
        /// Format
        /// </summary>
        private string Format(object obj, string category)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!string.IsNullOrEmpty(category))
                builder.AppendFormat("[{0}] ", category);
            if (obj is Exception)
            {
                var ex = (Exception)obj;
                builder.Append(ex.Message + "\r\n");
                builder.Append(ex.StackTrace + "\r\n");
            }
            else
            {
                builder.Append(obj.ToString() + "\r\n");
            }

            return builder.ToString();
        }
    }

    public class MyLog : ILoger
    {
        /// <summary>
        /// Single Instance
        /// </summary>
        private static MyLog instance;
        public static MyLog Instance
        {
            get
            {
                if (instance == null)
                    instance = new MyLog();
                return instance;
            }

        }

        /// <summary>
        /// Constructor
        /// </summary>
        private MyLog()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new LogerTraceListener());
        }

        public void Debug(object msg)
        {
            Trace.WriteLine(msg, "Debug");
        }

        public void Warn(object msg)
        {
            Trace.WriteLine(msg, "Warn");
        }

        public void Info(object msg)
        {
            Trace.WriteLine(msg, "Info");
        }

        public void Error(object msg)
        {
            Trace.WriteLine(msg, "Error");
        }
    }
}