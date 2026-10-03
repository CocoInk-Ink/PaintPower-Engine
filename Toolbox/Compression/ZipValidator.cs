using System;
using System.IO;
using System.IO.Compression;

public class ZipValidator
{
   public static bool IsZipValid(string filePath)
   {
       try
       {
           using (var zipFile = ZipFile.OpenRead(filePath))
           {
               // Accessing entries ensures the file is a valid ZIP
               var entries = zipFile.Entries;
               return true;
           }
       }
       catch (InvalidDataException)
       {
           // Invalid ZIP format or corrupted file
           return false;
       }
       catch (Exception)
       {
           // Handle other exceptions (e.g., file not found)
           return false;
       }
   }
   public static void Main()
   {
       string zipPath = @"C:\example\test.zip";
       Console.WriteLine(IsZipValid(zipPath) ? "Valid ZIP" : "Invalid ZIP");
   }
}