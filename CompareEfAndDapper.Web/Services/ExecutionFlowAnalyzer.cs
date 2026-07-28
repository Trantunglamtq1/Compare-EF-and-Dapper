namespace CompareEfAndDapper.Web.Services;

public class ExecutionStep
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Preparation", "Compilation", "Database", "Materialization"
    public string Description { get; set; } = string.Empty;
    public string InternalMechanism { get; set; } = string.Empty;
    public string CodeSnippet { get; set; } = string.Empty;
}

public class FrameworkFlowInfo
{
    public string FrameworkName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ExecutionStep> Steps { get; set; } = new();
}

public class ExecutionFlowAnalyzer
{
    public FrameworkFlowInfo GetEfCoreFlowInfo()
    {
        return new FrameworkFlowInfo
        {
            FrameworkName = "Entity Framework Core",
            Type = "Full-Featured ORM (Object-Relational Mapper)",
            Summary = "EF Core là một ORM đầy đủ tính năng. Mỗi câu truy vấn LINQ phải qua các bước: Duyệt cây biểu thức (Expression Tree) -> Dịch sang AST câu lệnh SQL -> Tra cứu Compiled Query Cache -> Thực thi ADO.NET -> Ép kiểu đối tượng (Materialization) -> Đăng ký vào Change Tracker.",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    StepNumber = 1,
                    Name = "Khởi tạo DbContext",
                    Category = "Preparation",
                    Description = "Ứng dụng lấy ra một instance của AppDbContext từ ServiceProvider (thường là Scoped).",
                    InternalMechanism = "EF Core khởi tạo Service Provider nội bộ, thiết lập các Interceptor và theo dõi transaction.",
                    CodeSnippet = "using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();"
                },
                new ExecutionStep
                {
                    StepNumber = 2,
                    Name = "Tra cứu Model Metadata (IModel)",
                    Category = "Preparation",
                    Description = "EF Core kiểm tra mô hình dữ liệu (Tables, Primary Keys, Relationships) đã được biên dịch chưa.",
                    InternalMechanism = "Mô hình IModel được xây dựng qua OnModelCreating và Fluent API. Mô hình này chỉ được tạo 1 lần duy nhất (Singleton) và cache lại trong AppDomain.",
                    CodeSnippet = "modelBuilder.Entity<Product>().HasOne(p => p.Category)..."
                },
                new ExecutionStep
                {
                    StepNumber = 3,
                    Name = "Phân tích LINQ Expression Tree",
                    Category = "Compilation",
                    Description = "C# compiler đóng gói câu lệnh LINQ thành một cây Expression Tree (System.Linq.Expressions).",
                    InternalMechanism = "EF Core chuyển đổi cây Expression Tree thành cấu trúc cây cú pháp SQL (SQL AST - Abstract Syntax Tree). Chi phí này tốn một lượng CPU nhỏ.",
                    CodeSnippet = "db.Products.Where(p => p.Price >= 100).Include(p => p.Category)"
                },
                new ExecutionStep
                {
                    StepNumber = 4,
                    Name = "Biên dịch câu lệnh SQL & Tra cứu Query Cache",
                    Category = "Compilation",
                    Description = "EF Core kiểm tra xem cấu trúc câu LINQ này đã từng sinh ra SQL chưa.",
                    InternalMechanism = "Nếu đã có trong Compiled Query Cache, EF lấy lại câu SQL mẫu và chỉ bind tham số mới. Nếu chưa có (Cache Miss), QueryCompiler sẽ biên dịch LINQ AST thành chuỗi SQL Parameterized.",
                    CodeSnippet = "SELECT [p].[Id], [p].[Name], [p].[Price] FROM [Products] AS [p] WHERE [p].[Price] >= @__minPrice_0"
                },
                new ExecutionStep
                {
                    StepNumber = 5,
                    Name = "Yêu cầu kết nối ADO.NET (Connection Pool)",
                    Category = "Database",
                    Description = "Lấy kết nối SqlConnection từ Connection Pool và thực thi DbCommand.",
                    InternalMechanism = "Sử dụng Microsoft.Data.SqlClient để mở kết nối (giao thức TDS - Tabular Data Stream qua TCP Port 1433 đến SQL Server) và gọi ExecuteReaderAsync().",
                    CodeSnippet = "await command.ExecuteReaderAsync();"
                },
                new ExecutionStep
                {
                    StepNumber = 6,
                    Name = "Materialization & Change Tracking",
                    Category = "Materialization",
                    Description = "Đọc từng dòng từ SqlDataReader và khởi tạo đối tượng Product trong C#.",
                    InternalMechanism = "EF Core tạo instance cho entity và tự động đính kèm vào Change Tracker (nếu không dùng AsNoTracking). Entity state được đánh dấu là 'Unchanged'.",
                    CodeSnippet = "var product = new Product { Id = reader.GetInt32(0), ... };\nDbContext.ChangeTracker.TrackEntity(product);"
                }
            }
        };
    }

    public FrameworkFlowInfo GetDapperFlowInfo()
    {
        return new FrameworkFlowInfo
        {
            FrameworkName = "Dapper",
            Type = "Micro-ORM (Extension Methods trên IDbConnection)",
            Summary = "Dapper là một Micro-ORM siêu nhẹ do StackOverflow phát triển. Dapper không dịch LINQ và không có Change Tracker. Thay vào đó, nó nhận trực tiếp SQL string và dùng System.Reflection.Emit (ILGenerator) để sinh ra mã C# IL động ép kiểu dữ liệu từ DataReader sang POCOs với tốc độ tiệm cận ADO.NET thuần.",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    StepNumber = 1,
                    Name = "Tạo kết nối IDbConnection",
                    Category = "Preparation",
                    Description = "Developer tự quản lý vòng đời kết nối (SqlConnection/SqliteConnection).",
                    InternalMechanism = "Dapper mở kết nối (nếu đang đóng) hoặc tái sử dụng kết nối IDbConnection do developer truyền vào.",
                    CodeSnippet = "using var connection = new SqlConnection(connectionString);"
                },
                new ExecutionStep
                {
                    StepNumber = 2,
                    Name = "Gửi trực tiếp câu SQL & Parameter Object",
                    Category = "Preparation",
                    Description = "Developer tự viết câu lệnh SQL thuần và đối tượng tham số.",
                    InternalMechanism = "Dapper không phân tích cú pháp LINQ hay tạo SQL AST. Chuỗi SQL được dùng trực tiếp làm chìa khóa Cache.",
                    CodeSnippet = "connection.QueryAsync<Product>(\"SELECT * FROM Products WHERE Price >= @Price\", new { Price = 100 });"
                },
                new ExecutionStep
                {
                    StepNumber = 3,
                    Name = "Tra cứu Cache IL Deserializer (Bí quyết của Dapper)",
                    Category = "Compilation",
                    Description = "Dapper kiểm tra xem đã có hàm map từ DataReader -> Product cho câu SQL này chưa.",
                    InternalMechanism = "Dapper tạo một Identity dựa trên (SqlString, CommandType, ParametersType, ReturnType, ConnectionType). Cache key này cực nhanh.",
                    CodeSnippet = "var deserializer = Cache.Get(identity);"
                },
                new ExecutionStep
                {
                    StepNumber = 4,
                    Name = "Sinh mã C# IL Động (System.Reflection.Emit) - Khi Cache Miss",
                    Category = "Compilation",
                    Description = "Nếu là lần đầu chạy truy vấn này, Dapper sử dụng DynamicMethod để tạo mã máy IL C#.",
                    InternalMechanism = "ILGenerator sinh ra chuỗi byte code C# tối ưu hóa ở mức IL để đọc giá trị từ DataReader theo đúng chỉ số column và gán trực tiếp vào property của POCO object.",
                    CodeSnippet = "DynamicMethod dm = new DynamicMethod(\"Deserializer\", typeof(Product), new[] { typeof(IDataReader) });\n// Emit IL code: Ldarg_0, Callvirt GetInt32, Stfld..."
                },
                new ExecutionStep
                {
                    StepNumber = 5,
                    Name = "Thực thi ADO.NET Command",
                    Category = "Database",
                    Description = "Tạo SqlCommand, bind tham số và gọi ExecuteReaderAsync().",
                    InternalMechanism = "Gửi gói tin TDS qua TCP Port 1433 trực tiếp tới SQL Server và nhận luồng dữ liệu SqlDataReader.",
                    CodeSnippet = "using var reader = await command.ExecuteReaderAsync();"
                },
                new ExecutionStep
                {
                    StepNumber = 6,
                    Name = "Fast Materialization (Zero Change Tracker)",
                    Category = "Materialization",
                    Description = "Thực thi compiled IL delegate để biến các dòng dữ liệu thành List<Product>.",
                    InternalMechanism = "Không qua Change Tracker, không lưu Identity Map, không kiểm tra trạng thái Entity. Đối tượng POCO được trả về trực tiếp và thu gom bởi GC khi không còn sử dụng.",
                    CodeSnippet = "while (reader.Read()) { yield return deserializer(reader); }"
                }
            }
        };
    }

    public FrameworkFlowInfo GetSqlCommandFlowInfo()
    {
        return new FrameworkFlowInfo
        {
            FrameworkName = "SQL Command (Raw ADO.NET)",
            Type = "Native ADO.NET Data Provider (Microsoft.Data.SqlClient)",
            Summary = "SQL Command (Raw ADO.NET) là lớp truy cập dữ liệu thấp nhất trong C# mà không qua bất kỳ ORM nào. Cài đặt trực tiếp SqlConnection, SqlCommand, SqlDataReader và tự tay đọc/ép kiểu từng cột (Manual Mapping).",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    StepNumber = 1,
                    Name = "Khởi tạo SqlConnection & SqlCommand",
                    Category = "Preparation",
                    Description = "Khởi tạo đối tượng SqlConnection và SqlCommand với chuỗi SQL và tham số.",
                    InternalMechanism = "Giao tiếp trực tiếp với ADO.NET Connection Pool để lấy kết nối Socket đến database server.",
                    CodeSnippet = "await using var connection = new SqlConnection(connStr);\nawait using var command = new SqlCommand(sql, connection);"
                },
                new ExecutionStep
                {
                    StepNumber = 2,
                    Name = "Gắn tham số SQL (SqlParameter)",
                    Category = "Preparation",
                    Description = "Tạo các đối tượng SqlParameter thủ công để chống SQL Injection.",
                    InternalMechanism = "Gắn kiểu dữ liệu SqlParameter (SqlDbType.Int, SqlDbType.NVarChar...) thẳng vào command.Parameters.",
                    CodeSnippet = "command.Parameters.AddWithValue(\"@Price\", 100);"
                },
                new ExecutionStep
                {
                    StepNumber = 3,
                    Name = "Không qua bước Biên dịch/Dịch LINQ/Reflection",
                    Category = "Compilation",
                    Description = "Bỏ qua hoàn toàn mọi tầng trung gian biên dịch của ORM.",
                    InternalMechanism = "Mã C# gửi trực tiếp chuỗi SQL dạng Parameterized Query tới máy chủ SQL Server mà không mất thời gian phân tích Expression Tree hay sinh IL Code.",
                    CodeSnippet = "// Zero ORM Compilation Overhead"
                },
                new ExecutionStep
                {
                    StepNumber = 4,
                    Name = "Thực thi DbCommand (ExecuteReaderAsync)",
                    Category = "Database",
                    Description = "Gửi lệnh SQL qua stream TCP port 1433 tới SQL Server.",
                    InternalMechanism = "SQL Server biên dịch câu SQL (Query Optimizer -> Execution Plan) và trả về luồng stream dữ liệu SqlDataReader.",
                    CodeSnippet = "await connection.OpenAsync();\nawait using var reader = await command.ExecuteReaderAsync();"
                },
                new ExecutionStep
                {
                    StepNumber = 5,
                    Name = "Materialization thủ công (Manual Loop & Read)",
                    Category = "Materialization",
                    Description = "Dùng vòng lặp while (reader.ReadAsync()) và đọc từng thuộc tính bằng Ordinal Index hoặc GetInt32/GetString.",
                    InternalMechanism = "Tự tay khởi tạo new Product() và gán giá trị từng column. Đây là phương thức đọc dữ liệu nhanh nhất và tốn ít RAM nhất.",
                    CodeSnippet = "while (await reader.ReadAsync()) {\n    list.Add(new Product {\n        Id = reader.GetInt32(0),\n        Name = reader.GetString(1)\n    });\n}"
                }
            }
        };
    }
}

