using System;
using System.Collections.Generic;
using System.Linq;
using Skender.Stock.Indicators;


namespace TradingBot.libCommon
{
	public static class CandleUtils
	{
		public static IEnumerable<Quote> ToQuotes( this IEnumerable<Candle> Candles )
			=> Candles.Select( q => new Quote()
			{
				Close = q.Close,
				High = q.Max,
				Low = q.Min,
				Open = q.Open,
				Date = q.CloseTime,
				Volume = q.Volume
			} );

		public static IEnumerable<Candle> ToCandles( this IEnumerable<Quote> Quotes )
			=> Quotes
			.Select( q => new Candle( TimeIntervalE.UNDEFINED,q.Date, q.Date, q.High, q.Low, q.Open, q.Close, q.Volume ) );

		public static bool IsBullish( this Candle Candle ) => Candle.Close > Candle.Open;

		public static bool IsBearish( this Candle Candle ) => Candle.Close < Candle.Open;
	}

	public static class OperationsUtils
	{
		public static IEnumerable<decimal?> GetSMA( this IEnumerable<decimal> source, int length, decimal? nullValue = null )
		{
			if( length <= 0 )
				throw new ArgumentException( "[GetSMA] La longitud debe ser mayor que cero." );

			var data = source.ToList();
			var result = new List<decimal?>();

			for( int k = 0; k < length - 1; k++ )
				result.Add( nullValue );

			for( int i = length - 1; i < data.Count; i++ )
			{
				decimal sum = 0;

				for( int j = i; j >= i - ( length - 1 ); j-- )
				{
					sum += data[ j ];
				}

				decimal average = sum / length;
				result.Add( average );
			}

			return result;
		}
	}
}
