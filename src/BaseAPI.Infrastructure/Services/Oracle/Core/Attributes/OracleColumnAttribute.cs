// — Definimos el espacio de nombres donde vive este archivo
namespace BaseAPI.Infrastructure.Services.Oracle.Core.Attributes;

// — Este atributo indica que [OracleColumn] solo se puede usar sobre propiedades
// — AllowMultiple=false significa que una propiedad solo puede tener UNA etiqueta [OracleColumn]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
// — Creamos una clase que hereda de Attribute (así C# sabe que es un atributo/etiqueta)
public class OracleColumnAttribute : Attribute
{
    // — Propiedad de solo lectura que guarda el nombre de la columna Oracle
    public string ColumnName { get; }

    // — Constructor: recibe el nombre de la columna Oracle como texto
    // — Ejemplo de uso: [OracleColumn("ID_ESTUDIANTE")]
    public OracleColumnAttribute(string columnName)
    {
        // — Guardamos el nombre recibido en la propiedad ColumnName
        ColumnName = columnName;
    }
}
