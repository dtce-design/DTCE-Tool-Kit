using System;

namespace BeamScheduleFormatter
{
	internal class AccessLease
	{
		private readonly Func<TimeSpan> now;

		private TimeSpan started;

		private TimeSpan validFor;

		private bool granted;

		public static readonly TimeSpan Duration = TimeSpan.FromHours(8.0);

		public bool IsValid
		{
			get
			{
				TimeSpan timeSpan = now() - started;
				if (granted && timeSpan >= TimeSpan.Zero)
				{
					return timeSpan < validFor;
				}
				return false;
			}
		}

		public AccessLease(Func<TimeSpan> clock)
		{
			now = clock;
		}

		public void Start()
		{
			Start(Duration);
		}

		public void Start(TimeSpan remaining)
		{
			Start(remaining, false);
		}

		public void Start(TimeSpan remaining, bool certification)
		{
			started = now();
			validFor = remaining;
			granted = remaining > TimeSpan.Zero && remaining <= (certification ? TimeSpan.FromDays(30.0) : Duration);
		}

		public void Reset()
		{
			granted = false;
		}
	}
}
