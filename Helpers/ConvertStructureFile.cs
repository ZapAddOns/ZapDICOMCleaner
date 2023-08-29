using System;
using System.Collections.Generic;

namespace ZapDICOMCleaner.Helpers
{
    public static class ConvertStructureFile
    {
        /// <summary>
        /// Convert a DS string with coordinates (x, y and z) consisting of numbers perhaps more than 16 characters 
        /// to coordinates consisting of numbers with max 16 characters
        /// </summary>
        /// <param name="input">Numbers as string</param>
        /// <returns>Numbers as string with max 16 characters per number</returns>
        public static string ConvertLongNumbers(int numOfPoints, string input)
        {
            var output = new List<string>();
            var entries = input.Split('\\');

            if (numOfPoints == 0)
            {
                throw new Exception("Structure without any points");
            }
            else if (numOfPoints * 3 != entries.Length)
            {
                throw new Exception("Wrong number of points");
            }
            else
            {
                foreach (var entry in entries)
                {
                    output.Add(ConvertLongNumber(entry));
                }
            }

            return string.Join("\\", output);
        }

        /// <summary>
        /// Convert a DS string with a number with length > 16 characters to one with max 16 characters
        /// </summary>
        /// <param name="input">Number as string</param>
        /// <returns>Number as string with max 16 characters</returns>
        public static string ConvertLongNumber(string input)
        {
            return input.Length > 16 ? input.Substring(0, 16) : input;
        }

        /// <summary>
        /// Remove a problem where a color consists of 4 values instead of 3
        /// </summary>
        /// <param name="input">String with 4 values</param>
        /// <returns>String with 3 values</returns>
        public static string ConvertColorString(string input)
        {
            var entries = input.Split('\\');
            var output = new List<string>(3);

            for (var i = 0; i < 3; i++)
            {
                output.Add(entries[i]);
            }

            return string.Join("\\", output); ;
        }
    }
}
