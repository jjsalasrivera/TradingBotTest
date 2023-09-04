using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingBot.libCommon;

namespace TradingBot.libAPIManager
{
	public interface IClientManager
	{
		public Task<IEnumerable<Candle>> GetHistoricAsync( TimeIntervalE intervalE, DateTime from, DateTime to, string symbol );

		public Task<string> GetSymbolInfoAsync( string symbol );
	}
}
