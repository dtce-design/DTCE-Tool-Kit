using System;
using System.Threading.Tasks;

namespace BeamScheduleFormatter
{
	internal sealed class StoreUpdatePolicy
	{
		public bool Blocked { get; private set; }

		public bool Checking { get; private set; }

		public DateTime NextCheckUtc { get; private set; }

		public async Task<bool> CheckAsync(Func<Task<StoreUpdateResult>> query, DateTime now, bool force = false)
		{
			if (Checking || (!force && now < NextCheckUtc))
			{
				return false;
			}
			Checking = true;
			try
			{
				StoreUpdateResult storeUpdateResult;
				try
				{
					storeUpdateResult = await query();
				}
				catch
				{
					storeUpdateResult = StoreUpdateResult.Unavailable;
				}
				Blocked = storeUpdateResult == StoreUpdateResult.Required;
				StoreUpdatePolicy storeUpdatePolicy = this;
				int num;
				switch (storeUpdateResult)
				{
				default:
					num = 15;
					break;
				case StoreUpdateResult.Required:
					num = 5;
					break;
				case StoreUpdateResult.Current:
					num = 360;
					break;
				}
				storeUpdatePolicy.NextCheckUtc = now.AddMinutes(num);
				return true;
			}
			finally
			{
				Checking = false;
			}
		}
	}
}
