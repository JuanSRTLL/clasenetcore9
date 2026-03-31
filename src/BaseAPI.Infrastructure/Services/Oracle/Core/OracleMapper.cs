// !! OracleColumnAttribute.cs — importamos la etiqueta [OracleColumn] que definimos en Attributes/OracleColumnAttribute.cs
// !! Sin este import, no podríamos usar GetCustomAttribute<OracleColumnAttribute>() más abajo
using BaseAPI.Infrastructure.Services.Oracle.Core.Attributes;
// — Importamos el sistema de logging para registrar advertencias
using Microsoft.Extensions.Logging;
// — Importamos las clases de Oracle para leer datos (OracleDataReader)
using Oracle.ManagedDataAccess.Client;
// — Importamos los tipos propios de Oracle (OracleDecimal, OracleString, etc.)
using Oracle.ManagedDataAccess.Types;
// — Importamos Reflection para inspeccionar propiedades de las clases C# en tiempo de ejecución
using System.Reflection;

// — Espacio de nombres de este archivo
namespace BaseAPI.Infrastructure.Services.Oracle.Core;

// — Clase que convierte filas de Oracle en objetos C# automáticamente
public class OracleMapper
{
    // — Campo privado que guarda el logger para registrar advertencias si algo falla al mapear
    private readonly ILogger<OracleMapper> _logger;

    // — Constructor: recibe el logger por inyección de dependencias
    public OracleMapper(ILogger<OracleMapper> logger)
    {
        // — Guardamos el logger que nos inyectaron para usarlo después
        _logger = logger;
    }

    // — Método principal: lee TODAS las filas del reader y retorna una lista de objetos T
    // — "where T : class, new()" significa: T debe ser una clase y tener constructor vacío
    public async Task<List<T>> MapAsync<T>(
        OracleDataReader reader,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        // — Creamos una lista vacía donde iremos agregando cada objeto mapeado
        var results = new List<T>();

        // — Obtenemos todas las propiedades públicas de la clase T que se puedan escribir
        // — BindingFlags.Public = solo públicas, Instance = no estáticas
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        // — Construimos un diccionario que vincula cada columna de Oracle con una propiedad de C#
        var columnMappings = BuildColumnMappings(reader, properties);

        // — Leemos fila por fila del reader (como leer un Excel línea por línea)
        while (await reader.ReadAsync(cancellationToken))
        {
            // — Para cada fila, creamos un nuevo objeto vacío de tipo T
            var item = new T();

            // — Recorremos cada par (índice de columna, propiedad C#) del diccionario
            foreach (var (columnIndex, property) in columnMappings)
            {
                // — Si el valor de esa columna NO es null en Oracle
                if (!reader.IsDBNull(columnIndex))
                {
                    try
                    {
                        // — Leemos el valor de la columna en esa fila
                        var value = reader.GetValue(columnIndex);
                        // — Convertimos el tipo de Oracle (OracleDecimal, etc.) al tipo de C# (int, string, etc.)
                        var convertedValue = ConvertOracleValue(value, property.PropertyType);
                        // — Asignamos el valor convertido a la propiedad del objeto C#
                        property.SetValue(item, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        // — Si hay error al mapear, registramos advertencia pero NO detenemos el proceso
                        _logger.LogWarning(ex,
                            "Error al mapear columna '{Column}' a propiedad '{Property}' en {Type}",
                            reader.GetName(columnIndex), property.Name, typeof(T).Name);
                    }
                }
            }

            // — Agregamos el objeto ya mapeado a la lista de resultados
            results.Add(item);
        }

        // — Retornamos la lista completa de objetos mapeados
        return results;
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTODO PRIVADO: construye el diccionario columna → propiedad
    // ═══════════════════════════════════════════════════════════════
    // — Este método se ejecuta UNA sola vez por consulta (no por cada fila)
    // — Devuelve un diccionario: { 0 → IdEstudiante, 1 → Nombre, 2 → ... }
    private Dictionary<int, PropertyInfo> BuildColumnMappings(
        OracleDataReader reader, List<PropertyInfo> properties)
    {
        // — Creamos el diccionario vacío donde guardaremos los mapeos
        var mappings = new Dictionary<int, PropertyInfo>();

        // — Recorremos cada columna que Oracle retornó (reader.FieldCount es la cantidad)
        for (int i = 0; i < reader.FieldCount; i++)
        {
            // — Obtenemos el nombre de la columna en la posición i (ej: "ID_ESTUDIANTE")
            var columnName = reader.GetName(i);

            // !! PRIORIDAD 1 — OracleColumnAttribute.cs se usa AQUÍ
            // !! Busca una propiedad que tenga [OracleColumn("ID_ESTUDIANTE")] (definido en OracleColumnAttribute.cs)
            var propByAttr = properties.FirstOrDefault(p =>
            {
                // !! OracleColumnAttribute.cs — GetCustomAttribute lee la etiqueta [OracleColumn] de la propiedad
                // !! Ejemplo: EstudianteOracleRow tiene [OracleColumn("ID_ESTUDIANTE")] sobre IdEstudiante
                // !!          → attr.ColumnName vale "ID_ESTUDIANTE"
                var attr = p.GetCustomAttribute<OracleColumnAttribute>();
                // !! OracleColumnAttribute.cs — attr.ColumnName es la propiedad que definimos en ese archivo
                return attr != null &&
                       string.Equals(attr.ColumnName, columnName, StringComparison.OrdinalIgnoreCase);
            });

            // — Si encontramos por atributo, lo guardamos y pasamos a la siguiente columna
            if (propByAttr != null) { mappings[i] = propByAttr; continue; }

            // — PRIORIDAD 2: Buscar propiedad cuyo nombre coincida directamente (ignorando mayúsculas)
            // — Ejemplo: columna "NOMBRE" → propiedad "Nombre"
            var propByName = properties.FirstOrDefault(p =>
                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            // — Si encontramos por nombre directo, lo guardamos y pasamos a la siguiente columna
            if (propByName != null) { mappings[i] = propByName; continue; }

            // — PRIORIDAD 3: Normalizar el nombre de la columna y buscar
            // — Ejemplo: "ANIO_MATRICULA" → "AnioMatricula" → buscar propiedad AnioMatricula
            var normalized = NormalizeColumnName(columnName);
            var propByNormalized = properties.FirstOrDefault(p =>
                string.Equals(p.Name, normalized, StringComparison.OrdinalIgnoreCase));

            // — Si encontramos por nombre normalizado, lo guardamos
            if (propByNormalized != null) { mappings[i] = propByNormalized; }
        }

        // — Retornamos el diccionario completo de mapeos columna → propiedad
        return mappings;
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTODO PRIVADO: convierte nombres de Oracle a PascalCase de C#
    // ═══════════════════════════════════════════════════════════════
    // — Ejemplo: "ANIO_MATRICULA" → ["ANIO", "MATRICULA"] → "Anio" + "Matricula" → "AnioMatricula"
    private static string NormalizeColumnName(string columnName)
    {
        // — Separamos por guion bajo: "ANIO_MATRICULA" → ["ANIO", "MATRICULA"]
        var parts = columnName.Split('_');
        // — Cada parte: primera letra mayúscula, resto minúscula, y las concatenamos
        return string.Concat(parts.Select(p =>
            p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : ""));
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTODO PRIVADO: convierte tipos de Oracle a tipos nativos de C#
    // ═══════════════════════════════════════════════════════════════
    // — Oracle tiene sus propios tipos: OracleDecimal (no es decimal de C#), OracleString, etc.
    // — Este método convierte esos tipos a los tipos que C# entiende (int, string, decimal, etc.)
    private static object? ConvertOracleValue(object value, Type targetType)
    {
        // — Si el valor es null o DBNull, retornamos null
        if (value == null || value == DBNull.Value) return null;

        // — Si el tipo destino es Nullable<int>, extraemos el tipo base (int)
        // — Nullable.GetUnderlyingType retorna int para int?, null para int (no nullable)
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // — Usamos switch de C# (pattern matching) para cada tipo de Oracle
        return value switch
        {
            // — OracleDecimal → int: cuando la propiedad C# es int
            OracleDecimal oracleDecimal when underlyingType == typeof(int) => oracleDecimal.ToInt32(),
            // — OracleDecimal → long: cuando la propiedad C# es long
            OracleDecimal oracleDecimal when underlyingType == typeof(long) => oracleDecimal.ToInt64(),
            // — OracleDecimal → decimal: cuando la propiedad C# es decimal
            OracleDecimal oracleDecimal when underlyingType == typeof(decimal) => oracleDecimal.Value,
            // — OracleDecimal → cualquier otro tipo numérico: convertimos via decimal
            OracleDecimal oracleDecimal => Convert.ChangeType((decimal)oracleDecimal.Value, underlyingType),
            // — OracleString → string: extraemos el valor string nativo
            OracleString oracleString => oracleString.Value,
            // — OracleDate → DateTime: extraemos la fecha nativa
            OracleDate oracleDate => oracleDate.Value,
            // — OracleTimeStamp → DateTime: extraemos el timestamp nativo
            OracleTimeStamp oracleTimeStamp => oracleTimeStamp.Value,
            // — Cualquier otro tipo: intentamos conversión genérica
            _ => Convert.ChangeType(value, underlyingType)
        };
    }
}
