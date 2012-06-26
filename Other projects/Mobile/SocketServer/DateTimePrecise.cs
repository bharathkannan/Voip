
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SocketServer
{
   /// Taken from CodeProject, actually gutted for our use, but the original is at:
   /// 
   /// http://www.codeproject.com/KB/cs/DateTimePrecise.aspx

   /// DateTimePrecise provides a way to get a DateTime that exhibits the
   /// relative precision of
   /// System.Diagnostics.Stopwatch, and the absolute accuracy of DateTime.Now.
   public class DateTimePrecise
   {
      /// Creates a new instance of DateTimePrecise.
      /// A large value of synchronizePeriodSeconds may cause arithmetic overthrow
      /// exceptions to be thrown. A small value may cause the time to be unstable.
      /// A good value is 10.
      /// synchronizePeriodSeconds = The number of seconds after which the
      /// DateTimePrecise will synchronize itself with the system clock.
      public DateTimePrecise()
      {
         Stopwatch = Stopwatch.StartNew();
         this.Stopwatch.Start();
         m_dtStart = DateTime.Now;
      }

      DateTime m_dtStart;
      Stopwatch Stopwatch = null;

      public DateTime Now
      {
         get
         {
            return m_dtStart.AddMilliseconds(this.Stopwatch.ElapsedMilliseconds);
         }
      }

   }
}
