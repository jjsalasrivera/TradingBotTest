using System.Collections.Generic;
using TradingBot.libCommon;

namespace TradingBot.libStrategies
{
    public interface IStrategy
    {
        public Order CheckForOperation(Position position);

        public void AddCandleToHistory( Candle candle );
    }
}
