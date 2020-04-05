# References 添加dll
# 引入命名空间 TongfuInn.CSV
# 使用CsvReader，CsvFile，CsvWriter操作Csv


1.CsvReader提供遍历整个Csv文件的方法，遍历期间，一直占用文件，需要及时Dispose()释放资源。
2.CsvFile是一个Csv文件的模型，可以使用CsvReader迅速为一个Csv文件创造一个CsvFile实例，该实例不占用文件资源。
3.CsvWriter能把一个CsvFile迅速写进磁盘，生成一个Csv文件。
4.DataTable可以辅助我们处理Csv文件，该库提供了响应的API。

示例1：
"",  hello    ,""""""
共有3列。
第一列：空字符串，即 String.Empty
第二列：hello，如果TrimColumns设置成true，则是hello。
第三列：""

示例2：
"","",,1,2   ,3,    ",    """   ,"","",
共有10列
第一列：空
第二列：空
第三列：空
第四列：1
第五列：2    长度为1
第六列：3长度为1
第七列：,    "   长度为6
第八列：空
第九列：空
第十列：空

# <center> 一、 遍历一个Csv文件
不修改CSV文件本身
## 使用CsvReader

```c#
using (CsvReader reader = new CsvReader(@"C:\test.csv",Encoding.UTF8))
{
// 如果Csv文件含有标题，则第一行读到的是标题
  while (reader.ReadNextRecord())
  {
​    Console.WriteLine(reader.Fields[0]);//记录的第一个字段
​    Console.WriteLine(reader.Fields[1]);//记录的第二个字段
  }
}
```

***注意点***
1. *必须使用using，它要释放文件资源。*
2. *CsvReader类是专门用来读一个Csv文件或Csv文件的流或Csv格式的字符串。*
3. *TrimColumns属性设置成true，则解析后的某个字段的字符串值会Trim();*
4. *可以利用API将Csv文件生成DataTable

# <center> 一、 修改或访问指定行列的Csv文件字段

```c#
CsvFile file = new CsvFile();
file.Load(@"C:\test.csv",true,false);
//遍历所有字段
foreach (CsvRecord record in file.Records)
{
    foreach (string field in record.Fields)
    {
        Console.WriteLine(field);
    }
}
//遍历所有标题
foreach (string header in file.Headers)
{
    Console.WriteLine(header);
}
//访问指定行数的记录
CsvRecord record2 = file[1];
//访问指定行数和列数的记录
string field2 = file[1, 2];
//访问指定行数和列名的记录
field2 = file[1, "Name"];

```

***注意点***
1. * CsvFile对象时不需要using，因为Load()加载Csv格式的数据源时，使用的是CsvReader，将CsvFile模型填充完毕，便调用了CsvReader的Dispose(); 
2. *我们可以在Csv的Header和CsvRecords中拿到任何我们想要的信息。
3. 通过Csv文件的索引器访问字段更方便。

```c#
public void Load(string filePath, bool hasHeaderRow, bool trimColumns)
```
*加载指定路径的Csv文件，第二个参数必须指定，它指示当前加载的文档是否有标题，true则把第一行加载到Headers属性，false，则Headers为空，HeaderCount = 0，Records记录中含有第一行。第三个参数，若为true，所有字段的字符串值会Trim()*

# <center> 三、生成一个新的Csv文件或为Csv文件增删改记录和标题

先创建一个CsvFile模型，再调用Write方法。记得用using。
打开开关，CarriageReturnAndLineFeedReplacement会替换换行符。
```c#
using(CsvWriter writer = new CsvWriter())

{
 CsvWriter.Write(CsvFile csvFile, Encoding encoding)
}
```

```c#
CsvFile file = new CsvFile();
//加载待修改的数据源
file.Load(@"C:\test.csv",true,false);
//添加标题
file.Headers.Add("Age");
//添加记录
CsvRecord record = new CsvRecord();
record.Fields.Add("hello");
//添加空字符串
record.Fields.Add("");
//添加两个逗号
record.Fields.Add(",,");
//修改指定行列的字段
record[0,1] = "10";
using(CsvWriter writer = new CsvWriter())
{
    writer.WriteCsv(file,"C:\\test.csv");
}
```