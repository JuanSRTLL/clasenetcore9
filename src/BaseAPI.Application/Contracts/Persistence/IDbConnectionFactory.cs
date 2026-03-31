// — Importamos DbConnection: clase abstracta que representa una conexión a cualquier BD
// — (OracleConnection, SqlConnection, NpgsqlConnection... todas heredan de DbConnection)
using System.Data.Common;

// — Espacio de nombres para los contratos de persistencia
namespace BaseAPI.Application.Contracts.Persistence;

// — Interfaz: define el contrato que debe cumplir cualquier fábrica de conexiones
// — Infrastructure la implementará con OracleConnection
// — Si cambiamos a PostgreSQL, solo creamos otra implementación — Application no cambia
public interface IDbConnectionFactory
{
    // — Crea y abre una NUEVA conexión a la base de datos
    // — Retorna DbConnection (genérico) para no acoplarnos a Oracle específicamente
    // — CancellationToken permite cancelar la operación si tarda mucho
    Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    // — Obtiene una conexión abierta inteligentemente:
    // —   Si hay transacción activa → reutiliza la conexión existente (misma transacción)
    // —   Si NO hay transacción → crea una nueva conexión independiente
    Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);
}
