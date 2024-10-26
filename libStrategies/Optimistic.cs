using System;
using System.Collections.Generic;
using Serilog;
using TradingBot.libCommon;
using TradingBot.libStrategies;

public class Optimistic : IStrategy
{
    private static readonly ILogger _logger = Log.ForContext<Optimistic>();
	List<Candle> _candles = new List<Candle>();
    int _maxElements = 200;
    DateTime _lastOperationTime = DateTime.MinValue;
    decimal _lastOperationValue = 0;
    decimal _percerntToSell = 0.02m;
    
    public Optimistic(decimal percerntToSell)
    {
        _percerntToSell = percerntToSell;
    }

    public void AddCandleToHistory(Candle candle)
    {
        _candles.Add( candle );

		if( _candles.Count > _maxElements )
			_candles.RemoveRange( 0, _candles.Count - _maxElements );
    }

    public Order CheckForOperation(Position position)
    {
        Order order = new Order( OrderTypeE.NONE );

        if( position.Postion == PositionE.OUT 
            && _lastOperationTime.AddDays( 1 ) <= _candles[^1].CloseTime 
            && _candles[^1].Close > _candles[^1].Open )
        {
            order = new Order( OrderTypeE.BUY);
            _lastOperationValue = _candles[^1].Close;
            _lastOperationTime = _candles[^1].CloseTime;
        }
        else if( position.Postion == PositionE.IN && _candles[^1].Close > _lastOperationValue * ( 1 + _percerntToSell ) )
        {
            order = new Order( OrderTypeE.SELL );
            _lastOperationTime = _candles[^1].CloseTime;
            _lastOperationValue = _candles[^1].Close;            
        }
        /*else if( position.Postion == PositionE.IN && _lastOperationTime.AddDays( 25 ) <= _candles[^1].CloseTime )
        {
            order = new Order( OrderTypeE.STOPLOSS, _lastOperationValue * 0.95m );
        }*/

        return order;
    }
}