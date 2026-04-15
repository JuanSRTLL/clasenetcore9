// — Importamos la interfaz del repositorio (Application, Clase 2)
// !! IEstadisticasRepository (Clase 2) — contrato que esta clase implementa
using BaseAPI.Application.Features.Estadisticas._Shared.Contracts;
// — Importamos los DTOs que los Handlers esperan recibir (Application, Clase 2)
// !! MatriculadosPorAnioDto, MatriculadosPorProgramaDto (Clase 2)
using BaseAPI.Application.Features.Estadisticas._Shared.DTOs;
// — Importamos los Procedures que ejecutan los stored procedures de Oracle
// !! MatriculadosPorAnioProcedure (sección 3), MatriculadosPorProgramaProcedure (sección 4)
using BaseAPI.Infrastructure.Features.Estadisticas.Procedures;

// — Espacio de nombres: Feature → Estadisticas (sin /Procedures)
namespace BaseAPI.Infrastructure.Features.Estadisticas;

// ═══════════════════════════════════════════════════════════════
// — EstadisticasRepository: implementación concreta del contrato
// ═══════════════════════════════════════════════════════════════
// !! IEstadisticasRepository (Clase 2, Application)
// — Mismo patrón que EstudiantesRepository (sección 5):
// —   delegar a Procedures + convertir OracleRow → Dto
// ═══════════════════════════════════════════════════════════════
public class EstadisticasRepository : IEstadisticasRepository
{
    // — _porAnio: procedure que consulta matriculados agrupados por año
    // !! MatriculadosPorAnioProcedure (sección 3)
    private readonly MatriculadosPorAnioProcedure _porAnio;
    // — _porPrograma: procedure que consulta matriculados por programa en un año
    // !! MatriculadosPorProgramaProcedure (sección 4)
    private readonly MatriculadosPorProgramaProcedure _porPrograma;

    // — Constructor: recibe los Procedures por DI
    public EstadisticasRepository(
        MatriculadosPorAnioProcedure porAnio,
        MatriculadosPorProgramaProcedure porPrograma)
    {
        _porAnio = porAnio;
        _porPrograma = porPrograma;
    }

    // ═══════════════════════════════════════════════════════════════
    // — ObtenerMatriculadosPorAnioAsync: matriculados agrupados por año
    // ═══════════════════════════════════════════════════════════════
    // !! IEstadisticasRepository.ObtenerMatriculadosPorAnioAsync() — contrato (Clase 2)
    public async Task<List<MatriculadosPorAnioDto>> ObtenerMatriculadosPorAnioAsync(
        CancellationToken cancellationToken = default)
    {
        // — Ejecutar procedure Oracle
        var rows = await _porAnio.ExecuteAsync(cancellationToken);

        // — Convertir OracleRow → Dto
        // — MatriculadosPorAnioOracleRow tiene: Anio, Cantidad
        // — MatriculadosPorAnioDto tiene: Anio, Cantidad
        // — Las propiedades coinciden pero son TIPOS DIFERENTES (OracleRow vs Dto)
        return rows.Select(row => new MatriculadosPorAnioDto
        {
            Anio = row.Anio,
            Cantidad = row.Cantidad
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // — ObtenerMatriculadosPorProgramaAsync: matriculados por programa para un año
    // ═══════════════════════════════════════════════════════════════
    // !! IEstadisticasRepository.ObtenerMatriculadosPorProgramaAsync(int) — contrato (Clase 2)
    public async Task<List<MatriculadosPorProgramaDto>> ObtenerMatriculadosPorProgramaAsync(
        int anio, CancellationToken cancellationToken = default)
    {
        // — Ejecutar procedure con el año como filtro
        // — El parámetro "anio" se pasa al procedure, que filtra por ANIO_MATRICULA = anio
        var rows = await _porPrograma.ExecuteAsync(anio, cancellationToken);

        // — Convertir OracleRow → Dto
        return rows.Select(row => new MatriculadosPorProgramaDto
        {
            Programa = row.Programa,
            Cantidad = row.Cantidad
        }).ToList();
    }
}