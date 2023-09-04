using System;
using System.Collections.Generic;
using System.Linq;
using Skender.Stock.Indicators;

namespace TradingBot.libCommon.Indicators
{
    public static class Indicators
    {
        public static IEnumerable<DPOResult> GetDPO( this IEnumerable<Candle> Candles, int Depth, bool isCentered = false )
        {
            List<DPOResult> dtoResults = new List<DPOResult>();

            if( Candles.Count() < Depth + 1 )
            {
                throw new ArgumentException( "La lista de candles debe tener al menos (period + 1) elementos." );
            }

            int barsback = Depth / 2 + 1;

            // Calcula la media móvil
            decimal[] ma = new decimal[ Candles.Count() ];
            for( int i = Depth; i < Candles.Count(); i++ )
            {
                decimal sum = 0;
                for( int j = i - Depth; j < i; j++ )
                {
                    sum += Candles.ElementAt( j ).Close;
                }
                ma[ i ] = sum / Depth;
            }

            // Calcula el DPO y crea los DTOResult
            for( int i = Depth + barsback; i < Candles.Count(); i++ )
            {
                decimal dpoValue = Candles.ElementAt( i ).Close - ma[ i - barsback ];
                dtoResults.Add( new DPOResult( dpoValue, Candles.ElementAt( i ).CloseTime ) );
            }

            return dtoResults;
        }

        public static IEnumerable<SSLResult> GetSSL( this IEnumerable<Candle> Candles, int Length )
        {
            List<SSLResult> results = new();

            var smaHigh = Candles.Select( c => c.Max ).GetSMA( Length, 0 );
            var smaLow = Candles.Select( c => c.Min ).GetSMA( Length, 0 );

            int hlv = 0;
            decimal sslDown = 0;
            decimal sslUp = 0;

            for( int i = 0; i < Candles.Count(); i++ )
            {
                Candle candle = Candles.ElementAt( i );

                if( candle.Close > smaHigh.ElementAt( i ) )
                    hlv = 1;
                else if( candle.Close < smaLow.ElementAt( i ) )
                    hlv = -1;

                if( hlv < 0 )
                {
                    sslDown = smaHigh.ElementAt( i )!.Value;
                    sslUp = smaLow.ElementAt( i )!.Value;
                }
                else
                {
                    sslDown = smaLow.ElementAt( i )!.Value;
                    sslUp = smaHigh.ElementAt( i )!.Value;
                }

                results.Add( new SSLResult( sslUp, sslDown, candle.CloseTime ) );
            }

            return results;
        }

        public static IEnumerable<GannHighLowResult> GetGannHighLow( this IEnumerable<Candle> candles, int hPeriod = 13, int lPeriod = 21 )
		{         
            decimal?[] smaHigh = candles.Select( c => c.Max ).GetSMA( hPeriod, 0 ).ToArray();
            decimal?[] smaLow = candles.Select( c => c.Min ).GetSMA( lPeriod, 0 ).ToArray();

            List<decimal> HLv = new();
            var HiLo = new List<GannHighLowResult>();


            for( int i = 0; i < candles.Count(); i++ )
            {
                decimal previousSmaHigh = i > 0 ? smaHigh[ i - 1 ].Value : 0;
                decimal previousSmaLow = i > 0 ? smaLow[ i - 1 ].Value : 0;

                var currentCandle = candles.ElementAt( i );
                decimal currentClose = currentCandle.Close;
                decimal currentSmaHigh = smaHigh[ i ].Value;
                decimal currentSmaLow = smaLow[ i ].Value;

                decimal currentHLd = currentClose > previousSmaHigh ? 1 : ( currentClose < previousSmaLow ? -1 : 0 );
                decimal previousHLv = i > 0 ? HLv[ i - 1 ] : 0;

                HLv.Add( currentHLd != 0 ? currentHLd : previousHLv );
                decimal currentHiLo = currentSmaLow;
                ColorE color = ColorE.GREEN;
                if( HLv[ i ] == -1 )
                {
                    currentHiLo = currentSmaHigh;
                    color = ColorE.RED;
                }
                HiLo.Add( new GannHighLowResult( currentHiLo, color, currentCandle.CloseTime ) );
            }

            return HiLo;
        }

        public static IEnumerable<StandardDeviationResult> GetVolumeStandardDeviation( this IEnumerable<Candle> Candles, int Length )
		{
            if( Candles.Count() < Length )
                throw new ArgumentException( $"Length ({Length}) is lowest than candles ({Candles.Count()})" );

            List<StandardDeviationResult> res = new();

            IEnumerable<(decimal, DateTime)> volumes = Candles.Reverse().Select( c => (c.Volume, c.CloseTime) );

            for(int i = 0; i < volumes.Count();i++ )
			{
                var take = i + Length < volumes.Count() ? Length : volumes.Count() - i;
                var sublist = volumes.Skip( i ).Take( take ).ToList();
                var (average, stDeviation) = StandardDeviation( sublist.Select( s => s.Item1 ) );
                res.Add( new StandardDeviationResult( average, average + stDeviation, average - stDeviation, volumes.ElementAt( i ).Item2 ) );

            }

            res.Reverse();

            return res;
        }

        public static IEnumerable<StandardDeviationResult> GetCandleDeviation( this IEnumerable<Candle> Candles, int Length )
		{
            if( Candles.Count() < Length )
                throw new ArgumentException( $"Length ({Length}) is lowest than candles ({Candles.Count()})" );

            List<StandardDeviationResult> res = new();

            IEnumerable<(decimal, DateTime)> candlesDiff = Candles.Reverse().Select( c => (Math.Abs( c.Open - c.Close ), c.CloseTime) );

            for( int i = 0; i < candlesDiff.Count(); i++ )
            {
                var take = i + Length < candlesDiff.Count() ? Length : candlesDiff.Count() - i;
                var sublist = candlesDiff.Skip( i ).Take( take ).ToList();
                var (average, stDeviation) = StandardDeviation( sublist.Select( s => s.Item1 ) );
                res.Add( new StandardDeviationResult( average, average + stDeviation, average - stDeviation, candlesDiff.ElementAt( i ).Item2 ) );

            }

            res.Reverse();

            return res;
        }

        static (decimal, decimal) StandardDeviation( IEnumerable<decimal> Values)
		{
            var average = Values.Average();
            var SumSquares = Values.Sum( x => ( x - average ) * ( x - average ) );
            var standardDeviation =  (decimal) Math.Sqrt( Values.Average( v => Math.Pow( (double) (v - average), 2 ) ) );

            return (average, standardDeviation);
        }
    }
}
