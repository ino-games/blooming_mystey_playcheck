using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.LogicCommon.Utils
{

    public class ArrayUtils
    {

        public static T[] GetRow<T>(T[,] matrix, int rowNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(0))
                    .Select(x => matrix[x, rowNumber])
                    .ToArray();
        }

        public static T[] GetColumn<T>(T[,] matrix, int columnNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(1))
                    .Select(x => matrix[columnNumber, x])
                    .ToArray();
        }

        internal static List<List<T>> ToList<T>(T[,] array)
        {
            var result = new List<List<T>>();
            for(var x = 0; x< array.GetLength(0); x++) 
            {
                result.Add(GetColumn(array, x).ToList());
            }

            return result;
        }

        internal static void InitArray<T>(T[,] array, T v)
        {
            for(var i = 0; i < array.GetLength(0); i++) {
                for(var j = 0; j < array.GetLength(1); j++) {
                    array[i, j] = v;
                }
            }
        }

        internal static TargetType[,] Transform<SourceType, TargetType>(SourceType[,] source, Func<SourceType, TargetType> transform) {
            var result = new TargetType[source.GetLength(0), source.GetLength(1)];
            for(var x = 0; x < source.GetLength(0); x++) {
                for(var y = 0; y < source.GetLength(1); y++) {
                    result[x, y] = transform(source[x,y]);
                }
            }
            return result;
        }

        internal static ArrayType[,] Clone<ArrayType>(ArrayType[,] source)
        {
            var clone = new ArrayType[source.GetLength(0), source.GetLength(1)];

            for(var x = 0; x < source.GetLength(0); x++) {
                for(var y = 0; y< source.GetLength(1); y++) {
                    clone[x,y] = source[x,y];
                }
            }

            return clone;
        }

         internal static ArrayType[] Clone<ArrayType>(ArrayType[] source)
        {
            var clone = new ArrayType[source.Length];

            for(var x = 0; x < source.Length; x++) {
                
                    clone[x] = source[x];
                
            }

            return clone;
        }


        internal static T[,] GetSlice<T>(T[,] source, int startX = 0, int endX = -1, int startY = 0, int endY = -1) {
            if(endX > source.GetLength(0))
                throw new Exception("endX out of bounds");

            if(endY > source.GetLength(1))
                throw new Exception("endY out of bounds");

            
            if(endX == -1) {
                endX = source.GetLength(0);
            }

            if(endY == -1) {
                endY = source.GetLength(1);
            }
            
            int xLength = endX - startX;
            int yLength = endY - startY;

            var result = new T[xLength, yLength];

            for(var x = startX; x< endX; x++) {
                for(var y = startY; y < endY; y++) {
                    result[x - startX, y - startY] = source[x, y];
                }
            }

            return result;
        }

        internal static T[,] ToArray<T>(List<List<T>> source)
        {
            int xDimension = source.Count();
            int yDimension = source.Max(list => list.Count());
            var result = new T[xDimension, yDimension];
            for(var x = 0; x < xDimension; x++) {
                for(var y = 0; y < yDimension; y++) {
                    result[x, y] = source[x][y];
                }
            }

            return result;
        }

        internal static List<List<T2>> MapList<T1, T2>(T1[,] array, Func<T1, T2> mapperFn)
        {
            return ToList(array).Select(innerList => innerList.Select(val => mapperFn(val)).ToList()).ToList();
        }

        internal static List<List<T2>> MapList<T1, T2>(List<List<T1>> list, Func<T1, T2> mapperFn)
        {
            return list.Select(innerList => innerList.Select(val => mapperFn(val)).ToList()).ToList();
        }
    }

    public class SliceOptions {

    }
}