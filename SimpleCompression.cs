using BrotliSharpLib;
using SevenZip;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.LZMA;
using System;
using System.IO;
using System.IO.Compression;

namespace SimpleCompressionLib
{
    public class SimpleCompression
    {
        private static int dictionary = 1 << 23;
        private static bool eos = false;

        private static CoderPropID[] propIDs =
            {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };

        private static object[] properties =
                {
                    (System.Int32)(dictionary),
                    (System.Int32)(2),
                    (System.Int32)(3),
                    (System.Int32)(0),
                    (System.Int32)(2),
                    (System.Int32)(128),
                    "bt4",
                    eos
                };

        public static byte[] Compress(byte[] data, SimpleCompressionMethod method = SimpleCompressionMethod.GZip)
        {
            switch (method)
            {
                case SimpleCompressionMethod.GZip:
                    using (MemoryStream result = new MemoryStream())
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        result.Write(lengthBytes, 0, 4);

                        using (GZipStream compressionStream = new GZipStream(result, CompressionMode.Compress))
                        {
                            compressionStream.Write(data, 0, data.Length);
                            compressionStream.Flush();
                        }

                        return result.ToArray();
                    }
                case SimpleCompressionMethod.Deflate:
                    using (MemoryStream result = new MemoryStream())
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        result.Write(lengthBytes, 0, 4);

                        using (DeflateStream compressionStream = new DeflateStream(result, CompressionMode.Compress))
                        {
                            compressionStream.Write(data, 0, data.Length);
                            compressionStream.Flush();
                        }

                        return result.ToArray();
                    }
                case SimpleCompressionMethod.Brotli:
                    return Brotli.CompressBuffer(data, 0, data.Length);
                case SimpleCompressionMethod.LZMA:
                    byte[] retVal = null;
                    SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
                    encoder.SetCoderProperties(propIDs, properties);

                    using (MemoryStream strmInStream = new MemoryStream(data))
                    {
                        using (MemoryStream strmOutStream = new MemoryStream())
                        {
                            encoder.WriteCoderProperties(strmOutStream);
                            long fileSize = strmInStream.Length;

                            for (int i = 0; i < 8; i++)
                            {
                                strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));
                            }

                            encoder.Code(strmInStream, strmOutStream, -1, -1, null);
                            retVal = strmOutStream.ToArray();
                        }
                    }

                    return retVal;
                case SimpleCompressionMethod.Zlib:
                    using (MemoryStream result = new MemoryStream())
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        result.Write(lengthBytes, 0, 4);

                        using (Ionic.Zlib.ZlibStream compressionStream = new Ionic.Zlib.ZlibStream(result, Ionic.Zlib.CompressionMode.Compress))
                        {
                            compressionStream.Write(data, 0, data.Length);
                            compressionStream.Flush();
                        }

                        return result.ToArray();
                    }
                case SimpleCompressionMethod.Zip:
                    using (MemoryStream compressedStream = new MemoryStream())
                    {
                        using (ZipArchive archive = new ZipArchive(compressedStream, ZipArchiveMode.Create))
                        {
                            ZipArchiveEntry entry = archive.CreateEntry("data", CompressionLevel.Optimal);

                            using (Stream entryStream = entry.Open())
                            {
                                entryStream.Write(data, 0, data.Length);
                            }
                        }

                        return compressedStream.ToArray();
                    }
                case SimpleCompressionMethod.BZip2:
                    using (MemoryStream result = new MemoryStream())
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        result.Write(lengthBytes, 0, 4);

                        using (BZip2Stream compressionStream = new BZip2Stream(result, SharpCompress.Compressors.CompressionMode.Compress, false))
                        {
                            compressionStream.Write(data, 0, data.Length);
                            compressionStream.Flush();
                        }

                        return result.ToArray();
                    }
                case SimpleCompressionMethod.LZip:
                    using (MemoryStream result = new MemoryStream())
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        result.Write(lengthBytes, 0, 4);

                        using (LZipStream compressionStream = new LZipStream(result, SharpCompress.Compressors.CompressionMode.Compress))
                        {
                            compressionStream.Write(data, 0, data.Length);
                            compressionStream.Flush();
                        }

                        return result.ToArray();
                    }
            }

            throw new Exception("Invalid value in the \"method\" parameter has specified.");
        }

        public static byte[] Decompress(byte[] data, SimpleCompressionMethod method = SimpleCompressionMethod.GZip)
        {
            switch (method)
            {
                case SimpleCompressionMethod.GZip:
                    using (MemoryStream source = new MemoryStream(data))
                    {
                        byte[] lengthBytes = new byte[4];
                        source.Read(lengthBytes, 0, 4);
                        int length = BitConverter.ToInt32(lengthBytes, 0);

                        using (GZipStream decompressionStream = new GZipStream(source, CompressionMode.Decompress))
                        {
                            var result = new byte[length];
                            int totalRead = 0, bytesRead;

                            while ((bytesRead = decompressionStream.Read(result, totalRead, length - totalRead)) > 0)
                            {
                                totalRead += bytesRead;
                            }

                            return result;
                        }
                    }
                case SimpleCompressionMethod.Deflate:
                    using (MemoryStream source = new MemoryStream(data))
                    {
                        byte[] lengthBytes = new byte[4];
                        source.Read(lengthBytes, 0, 4);
                        int length = BitConverter.ToInt32(lengthBytes, 0);

                        using (DeflateStream decompressionStream = new DeflateStream(source, CompressionMode.Decompress))
                        {
                            var result = new byte[length];
                            int totalRead = 0, bytesRead;

                            while ((bytesRead = decompressionStream.Read(result, totalRead, length - totalRead)) > 0)
                            {
                                totalRead += bytesRead;
                            }

                            return result;
                        }
                    }
                case SimpleCompressionMethod.Brotli:
                    return Brotli.DecompressBuffer(data, 0, data.Length);
                case SimpleCompressionMethod.LZMA:
                    byte[] retVal = null;
                    SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

                    using (MemoryStream strmInStream = new MemoryStream(data))
                    {
                        strmInStream.Seek(0, 0);

                        using (MemoryStream strmOutStream = new MemoryStream())
                        {
                            byte[] properties2 = new byte[5];

                            if (strmInStream.Read(properties2, 0, 5) != 5)
                            {
                                throw (new Exception("input .lzma is too short"));
                            }

                            long outSize = 0;

                            for (int i = 0; i < 8; i++)
                            {
                                int v = strmInStream.ReadByte();

                                if (v < 0)
                                {
                                    throw (new Exception("Can't Read 1"));
                                }

                                outSize |= ((long)(byte)v) << (8 * i);
                            }

                            decoder.SetDecoderProperties(properties2);

                            long compressedSize = strmInStream.Length - strmInStream.Position;
                            decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

                            retVal = strmOutStream.ToArray();
                        }
                    }

                    return retVal;
                case SimpleCompressionMethod.Zlib:
                    using (MemoryStream source = new MemoryStream(data))
                    {
                        byte[] lengthBytes = new byte[4];
                        source.Read(lengthBytes, 0, 4);
                        int length = BitConverter.ToInt32(lengthBytes, 0);

                        using (Ionic.Zlib.ZlibStream decompressionStream = new Ionic.Zlib.ZlibStream(source, Ionic.Zlib.CompressionMode.Decompress))
                        {
                            var result = new byte[length];
                            int totalRead = 0, bytesRead;

                            while ((bytesRead = decompressionStream.Read(result, totalRead, length - totalRead)) > 0)
                            {
                                totalRead += bytesRead;
                            }

                            return result;
                        }
                    }
                case SimpleCompressionMethod.Zip:
                    using (MemoryStream zipStream = new MemoryStream(data))
                    {
                        using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                        {
                            ZipArchiveEntry entry = archive.Entries[0];

                            using (Stream entryStream = entry.Open())
                            {
                                using (MemoryStream decompressedStream = new MemoryStream())
                                {
                                    entryStream.CopyTo(decompressedStream);
                                    return decompressedStream.ToArray();
                                }
                            }
                        }
                    }
                case SimpleCompressionMethod.BZip2:
                    using (MemoryStream source = new MemoryStream(data))
                    {
                        byte[] lengthBytes = new byte[4];
                        source.Read(lengthBytes, 0, 4);
                        int length = BitConverter.ToInt32(lengthBytes, 0);

                        using (BZip2Stream decompressionStream = new BZip2Stream(source, SharpCompress.Compressors.CompressionMode.Decompress, false))
                        {
                            var result = new byte[length];
                            int totalRead = 0, bytesRead;

                            while ((bytesRead = decompressionStream.Read(result, totalRead, length - totalRead)) > 0)
                            {
                                totalRead += bytesRead;
                            }

                            return result;
                        }
                    }
                case SimpleCompressionMethod.LZip:
                    using (MemoryStream source = new MemoryStream(data))
                    {
                        byte[] lengthBytes = new byte[4];
                        source.Read(lengthBytes, 0, 4);
                        int length = BitConverter.ToInt32(lengthBytes, 0);

                        using (LZipStream decompressionStream = new LZipStream(source, SharpCompress.Compressors.CompressionMode.Decompress))
                        {
                            var result = new byte[length];
                            int totalRead = 0, bytesRead;

                            while ((bytesRead = decompressionStream.Read(result, totalRead, length - totalRead)) > 0)
                            {
                                totalRead += bytesRead;
                            }

                            return result;
                        }
                    }
            }
            
            throw new Exception("Invalid value in the \"method\" parameter has specified.");
        }
    }

    public enum SimpleCompressionMethod
    {
        GZip,
        Deflate,
        Brotli,
        LZMA,
        Zlib,
        Zip,
        BZip2,
        LZip
    }
}