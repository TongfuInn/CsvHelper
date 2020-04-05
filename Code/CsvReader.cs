using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace TongfuInn.CSV
{
    /// <summary>
    /// 从各种类型的数据源读取CSV格式的内容
    /// </summary>
    public sealed class CsvReader : IDisposable
    {
        #region Members

        private FileStream _fileStream;
        private Stream _stream;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private Stream _memoryStream;
        private Encoding _encoding;
        private readonly StringBuilder _columnBuilder = new StringBuilder(100);
        private readonly Type _type = Type.File;

        private bool _dispose = false;
        #endregion Members

        #region Properties

        /// <summary>
        /// 设置成true，会自动去除读到的字段的值两端的空格。 如 "hello world ",实际读到的是[hello world],而不是[hello world ].
        /// </summary>
        public bool TrimColumns { get; set; } = true;

        /// <summary>
        /// 指示CSV有无标题行
        /// </summary>
        public bool HasHeaderRow { get; set; }

        /// <summary>
        /// 返回当前正在被读取的行的所有字段的值的集合。初始值为null
        /// </summary>
        public List<string> Fields { get; private set; }

        /// <summary>
        /// 返回当前正在被读取的行的字段的数量。未读取时，为null
        /// </summary>
        public int? FieldCount
        {
            get
            {
                return (Fields != null ? Fields.Count : (int?)null);
            }
        }

        #endregion Properties

        #region Enums

        /// <summary>
        /// 数据源类型
        /// </summary>
        private enum Type
        {
            File,
            Stream,
            String
        }

        #endregion Enums

        #region Constructors

        /// <summary>
        /// 文件
        /// </summary>
        /// <param name="filePath">File path</param>
        public CsvReader(string filePath)
        {
            _type = Type.File;
            Initialise(filePath, Encoding.Default);
        }

        /// <summary>
        /// 文件
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="encoding">Encoding</param>
        public CsvReader(string filePath, Encoding encoding)
        {
            _type = Type.File;
            Initialise(filePath, encoding);
        }

        /// <summary>
        /// 流
        /// </summary>
        /// <param name="stream">Stream</param>
        public CsvReader(Stream stream)
        {
            _type = Type.Stream;
            Initialise(stream, Encoding.Default);
        }

        /// <summary>
        /// 流
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="encoding">Encoding</param>
        public CsvReader(Stream stream, Encoding encoding)
        {
            _type = Type.Stream;
            Initialise(stream, encoding);
        }

        /// <summary>
        /// csv字符串
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="csvContent"></param>
        public CsvReader(Encoding encoding, string csvContent)
        {
            _type = Type.String;
            Initialise(encoding, csvContent);  
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// 文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="encoding"></param>
        private void Initialise(string filePath, Encoding encoding)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(string.Format("The file '{0}' does not exist.", filePath));

            _fileStream = File.OpenRead(filePath);
            Initialise(_fileStream, encoding);
        }

        /// <summary>
        /// 流
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        private void Initialise(Stream stream, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException("The supplied stream is null.");

            _stream = stream;
            _stream.Position = 0;
            _encoding = (encoding ?? Encoding.Default);
            _streamReader = new StreamReader(_stream, _encoding);
        }

        /// <summary>
        /// csv字符串
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="csvContent"></param>
        private void Initialise(Encoding encoding, string csvContent)
        {
            if (csvContent == null)
                throw new ArgumentNullException("The supplied csvContent is null.");

            _encoding = (encoding ?? Encoding.Default);

            _memoryStream = new MemoryStream(csvContent.Length);
            _streamWriter = new StreamWriter(_memoryStream);
            _streamWriter.Write(csvContent);
            _streamWriter.Flush();
            Initialise(_memoryStream, encoding);           
        }

        /// <summary>
        /// 读下一行记录
        /// </summary>
        /// <returns>如果成功读取一行记录，返回true，否则返回false</returns>
        public bool ReadNextRecord()
        {
            Fields = null;
            string line = _streamReader.ReadLine();
            while(line != null && line.Replace(" ","").Length == 0)
            {
                line = _streamReader.ReadLine();
            }
            if (line == null)
                return false;

            ParseLine(line);
            return true;
        }

        public DataTable ReadIntoDataTable()
        {
            return ReadIntoDataTable(new System.Type[] {});
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnTypes">列类型。如果提供类型数组长度小于标题数目，后面的标题默认是string</param>
        /// <returns></returns>
        public DataTable ReadIntoDataTable(System.Type[] columnTypes)
        {
            DataTable dataTable = new DataTable();
            bool addedHeader = false;
            _stream.Position = 0;

            while (ReadNextRecord())
            {
                if (!addedHeader)
                {
                    for (int i = 0; i < Fields.Count; i++)
                        dataTable.Columns.Add(Fields[i], (columnTypes.Length > i ? columnTypes[i] : typeof(string)));

                    addedHeader = true;
                    continue;
                }

                DataRow row = dataTable.NewRow();

                for (int i = 0; i < Fields.Count; i++)
                    row[i] = Fields[i];

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        /// <summary>
        /// 解析csv行
        /// </summary>
        /// <param name="line">Line</param>
        private void ParseLine(string line)
        {
            Fields = new List<string>();
            _columnBuilder.Remove(0, _columnBuilder.Length);

            bool inColumn = false;
            bool inQuotes = false;
            int quoteCount = 0;

            for (int i = 0; i < line.Length; ++i)
            {
                char character = line[i];

                //未进入列
                if (!inColumn)
                {
                    //去除列前的所有空格
                    if (character == ' ')
                    {
                        continue;
                    }
                    //第一次遇到的非空白字符若是逗号，则认为该列值是空字符串
                    else if (character == ',')
                    {
                        Fields.Add(string.Empty);
                        inColumn = false;
                        inQuotes = false;
                        continue;
                    }
                    //第一次遇到的非空白字符若是双引号，则认为进入了列，且该列是必须用双引号括起来的特殊列
                    else if (character == '"')
                    {
                        inColumn = true;
                        inQuotes = true;
                        quoteCount++;
                        continue;
                    }
                    //其他字符则标记进入了列，但是是不需要用双引号括起来的普通列
                    else
                    {
                        inColumn = true;
                        inQuotes = false;
                    }
                }
                else // 进入列
                {
                    //如果是被双引号包含的特殊列
                    if (inQuotes)
                    {
                        // 双引号计数器必定为奇数
                        if (quoteCount % 2 == 0)
                        {
                            throw new FormatException("Invalid double quotes");
                        }
                        //如果是一个列的最后一个双引号
                        if (character == '"' && (i + 1) < line.Length &&  line[i + 1] != '"')
                        {
                            quoteCount++;
                            //去除结束双引号与逗号之间的无效空白字符
                            for (int j = 1; ; ++j)
                            {
                                if ((i + j) < line.Length && line[i + j] == ',')
                                {
                                    i = i + j;
                                    inColumn = false;
                                    break;
                                }
                                else if ((i + j) < line.Length && line[i + j] == ' ')
                                {
                                    ;//空语句
                                }
                                else if(i + j == line.Length)
                                {
                                    i = i + j;
                                    break;
                                }
                            }
                        }
                        //处理双引号。 列中的双引号必须成对且无间隔的出现
                        else if (character == '"' && (i + 1) < line.Length && line[i + 1] == '"')
                        {
                                ++i;
                                quoteCount = quoteCount + 2;
                        }
                        else if(character == '"' && i + 1 == line.Length)
                        {
                            quoteCount++;
                            inColumn = false;
                        }
                    }

                    else //处理普通列
                    {
                        if (character == ',')
                        {
                            quoteCount = 0;
                            inColumn = false;
                        }
                        else if(character == '"')
                        {
                            throw new FormatException("This field cant't contatin double quotes because there aren't double quotes at both ends of field");
                        }
                    }
                }

                if (!inColumn)
                {
                    string field = inQuotes ? _columnBuilder.ToString() : _columnBuilder.ToString().Trim();
                    Fields.Add(TrimColumns ? field.Trim() : field);
                    _columnBuilder.Remove(0, _columnBuilder.Length);
                    inQuotes = false;
                    quoteCount = 0;
                }
                else
                    _columnBuilder.Append(character);
            }

            //处理最后一列
            if (inColumn)
            {
                if (inQuotes)
                {
                    if (quoteCount % 2 != 0)
                    {
                        throw new FormatException("Invalid double quotes");
                    }
                }
                else
                {
                    if(quoteCount != 0)
                    {
                        throw new FormatException("Invalid double quotes");
                    }
                }
                Fields.Add(TrimColumns ? _columnBuilder.ToString().Trim() : _columnBuilder.ToString());
                inColumn = false;
                quoteCount = 0;
            }

            if(!inColumn && line.Trim().EndsWith(","))
            {
                Fields.Add(string.Empty);
            }
        }


        public void Dispose()
        {
            if (_dispose)
            {
                _dispose = true;
                CleanUp();
                GC.SuppressFinalize(this);
            }
        }

        ~CsvReader()
        {
            CleanUp();
        }


        /// <summary>
        /// 释放非托管资源
        /// </summary>
        private void CleanUp()
        {
            if (_streamReader != null)
            {
                _streamReader.Close();
                _streamReader.Dispose();
            }

            if (_streamWriter != null)
            {
                _streamWriter.Close();
                _streamWriter.Dispose();
            }

            if (_memoryStream != null)
            {
                _memoryStream.Close();
                _memoryStream.Dispose();
            }

            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream.Dispose();
            }

            if ((_type == Type.String || _type == Type.File) && _stream != null)
            {
                _stream.Close();
                _stream.Dispose();
            }
        }
        #endregion Methods
    }
}
