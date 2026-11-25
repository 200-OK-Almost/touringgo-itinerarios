using Itinerario.Application.DTOs;
using Itinerario.Application.DTOs.Gastos;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

public class GastosService : IGastosService
{
    private readonly IGastosRepository _gastosRepository;

    public GastosService(IGastosRepository gastosRepository)
    {
        _gastosRepository = gastosRepository;
    }

    public async Task<GastoDto> ActualizarAsync(
        Guid viajeId,
        Guid gastoId,
        Guid usuarioActualId,
        ActualizarGastoRequestDto request,
        CancellationToken ct = default)
    {
        var gasto = await _gastosRepository.ObtenerPorIdAsync(
            viajeId, gastoId, ct);

        if (gasto is null)
            throw new KeyNotFoundException("No se encontró el gasto.");

        ValidarPuedeModificar(gasto, usuarioActualId);

        if (gasto.LiquidacionId.HasValue)
            throw new InvalidOperationException(
                "No se puede modificar un gasto liquidado.");

        ValidarDatosBasicos(
            request.Concepto,
            request.Monto,
            request.Moneda,
            request.FechaGasto);

        await ValidarMiembrosAsync(
            viajeId,
            usuarioActualId,
            request.PagadoPorUsuarioId,
            request.ParticipantesIds,
            ct);

        gasto.Concepto = request.Concepto.Trim();
        gasto.Monto = request.Monto;
        gasto.Moneda = request.Moneda;
        gasto.PagadoPorMiembroId = request.PagadoPorUsuarioId;
        gasto.FechaGasto = request.FechaGasto;

        // Reemplazamos las divisiones anteriores.
        gasto.Divisiones.Clear();

        var nuevasDivisiones = ConstruirDivisiones(
            viajeId,
            gasto.Id,
            request.Monto,
            request.ParticipantesIds);

        foreach (var division in nuevasDivisiones)
        {
            gasto.Divisiones.Add(division);
        }

        await _gastosRepository.GuardarCambiosAsync(ct);

        return MapearGastoDto(gasto);
    }

    public async Task<GastoDto> CrearAsync(Guid viajeId, Guid usuarioActualId, CrearGastoRequestDto request, CancellationToken ct = default)
    {
        ValidarDatosBasicos(
            request.Concepto,
            request.Monto,
            request.Moneda,
            request.FechaGasto);

        var participantes = request.ParticipantesIds ?? throw new ArgumentException("Debe indicar los participantes.");

        await ValidarMiembrosAsync(
            viajeId,
            usuarioActualId,
            request.PagadoPorUsuarioId,
            participantes,
            ct);

        var gastoId = Guid.NewGuid();

        var gasto = new Gasto
        {
            Id = gastoId,
            ViajeId = viajeId,
            Concepto = request.Concepto.Trim(),
            Monto = request.Monto,
            Moneda = request.Moneda,
            PagadoPorMiembroId = request.PagadoPorUsuarioId,
            RegistradoPorMiembroId = usuarioActualId,
            FechaGasto = request.FechaGasto,
            Divisiones = ConstruirDivisiones(
                viajeId,
                gastoId,
                request.Monto,
                participantes)
        };

        _gastosRepository.Agregar(gasto);

        await _gastosRepository.GuardarCambiosAsync(ct);

        return MapearGastoDto(gasto);
    }


    public async Task EliminarAsync(
        Guid viajeId,
        Guid gastoId,
        Guid usuarioActualId,
        CancellationToken ct = default)
    {
        var gasto = await _gastosRepository.ObtenerPorIdAsync(
            viajeId, gastoId, ct);

        if (gasto is null)
            throw new NotFoundException("No se encontró el gasto.");

        ValidarPuedeModificar(gasto, usuarioActualId);

        if (gasto.LiquidacionId.HasValue)
            throw new InvalidOperationException("No se puede eliminar un gasto liquidado.");

        _gastosRepository.Eliminar(gasto);

        await _gastosRepository.GuardarCambiosAsync(ct);
    }


    public async Task<LiquidacionDto> LiquidarAsync(
        Guid viajeId,
        Guid usuarioActualId,
        LiquidarGastosRequestDto request,
        CancellationToken ct = default)
    {
        var datos = await PrepararLiquidacionAsync(
            viajeId,
            usuarioActualId,
            request,
            ct);

        var liquidacion = new Liquidacion
        {
            Id = Guid.NewGuid(),
            ViajeId = viajeId,
            RealizadaPorUsuarioId = usuarioActualId,
            Moneda = datos.Moneda,
            FechaLiquidacion = DateTime.UtcNow,
            Transferencias = datos.Transferencias
        };

        // Asociar todos los gastos a la liquidación.
        foreach (var gasto in datos.Gastos)
        {
            gasto.LiquidacionId = liquidacion.Id;
        }

        // Agregar la liquidación y persistir todos los cambios.
        _gastosRepository.AgregarLiquidacion(liquidacion);

        await _gastosRepository.GuardarCambiosAsync(ct);

        return new LiquidacionDto
        {
            Id = liquidacion.Id,
            ViajeId = liquidacion.ViajeId,
            RealizadaPorUsuarioId = liquidacion.RealizadaPorUsuarioId,
            Moneda = liquidacion.Moneda,
            FechaLiquidacion = liquidacion.FechaLiquidacion,

            GastoIds = datos.Gastos
                .Select(g => g.Id)
                .ToArray(),

            Transferencias = MapearTransferenciasDto(
                liquidacion.Transferencias
            )
        };
    }

    public async Task<ResultadoPaginadoDto<GastoDto>> ListarAsync(
        Guid viajeId,
        Guid usuarioActualId,
        FiltroGastosDto filtro,
        CancellationToken ct = default)
    {
        await ValidarMiembroAsync(viajeId, usuarioActualId, ct);

        if (filtro.Pagina < 1 ||
            filtro.Tamano < 1 ||
            filtro.Tamano > 100)
        {
            throw new ArgumentException("Parámetros de paginación inválidos.");
        }

        if (filtro.FechaDesde.HasValue &&
            filtro.FechaHasta.HasValue &&
            filtro.FechaDesde > filtro.FechaHasta)
        {
            throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");
        }

        var resultado = await _gastosRepository.ListarAsync(
            viajeId, filtro, ct);

        var items = resultado.Items
            .Select(MapearGastoDto)
            .ToList();

        var totalPaginas = (int)Math.Ceiling(
            resultado.Total / (double)filtro.Tamano);

        return new ResultadoPaginadoDto<GastoDto>
        {
            Items = items,
            Pagina = filtro.Pagina,
            Tamano = filtro.Tamano,
            Total = resultado.Total
        };
    }

    public async Task<BalanceGastosDto> ObtenerBalanceAsync(
        Guid viajeId,
        Guid usuarioActualId,
        TipoMoneda moneda,
        CancellationToken ct = default)
    {
        await ValidarMiembroAsync(viajeId, usuarioActualId, ct);

        if (!Enum.IsDefined<TipoMoneda>(moneda))
            throw new ArgumentException("Moneda inválida.");

        var pagado = await _gastosRepository
            .ObtenerTotalPagadoPendienteAsync(viajeId, moneda, ct);

        var asignado = await _gastosRepository
            .ObtenerTotalAsignadoPendienteAsync(viajeId, moneda, ct);

        var usuarioIds = pagado.Keys
            .Union(asignado.Keys)
            .Distinct();

        var balances = usuarioIds.Select(usuarioId =>
        {
            var totalPagado = pagado.GetValueOrDefault(usuarioId);
            var totalAsignado = asignado.GetValueOrDefault(usuarioId);

            return new BalanceParticipanteDto {
                UsuarioId = usuarioId,
                TotalPagado = totalPagado,
                TotalAsignado = totalAsignado,
                BalanceNeto  = totalPagado - totalAsignado
            };
        }).ToList();

        return new BalanceGastosDto
        {
            ViajeId = viajeId,
            Moneda = moneda,
            Balances = balances
        };
    }

    public async Task<GastoDto> ObtenerPorIdAsync(
        Guid viajeId,
        Guid gastoId,
        Guid usuarioActualId,
        CancellationToken ct = default)
    {
        await ValidarMiembroAsync(viajeId, usuarioActualId, ct);

        var gasto = await _gastosRepository.ObtenerPorIdAsync(viajeId, gastoId, ct);

        if (gasto is null)
            throw new NotFoundException("No se encontró el gasto.");

        return MapearGastoDto(gasto);
    }

    public async Task<PrevisualizacionLiquidacionDto>
    PrevisualizarLiquidacionAsync(
        Guid viajeId,
        Guid usuarioActualId,
        LiquidarGastosRequestDto request,
        CancellationToken ct = default)
    {
        var datos = await PrepararLiquidacionAsync(
            viajeId,
            usuarioActualId,
            request,
            ct);

        return new PrevisualizacionLiquidacionDto
        {
            ViajeId = viajeId,
            Moneda = datos.Moneda,

            GastoIds = datos.Gastos
                .Select(g => g.Id)
                .ToArray(),

            Transferencias = MapearTransferenciasDto(
                datos.Transferencias
            )
        };
    }

    // -----------------------
    // Helpers
    // -----------------------

    // Record para liquidación 
    private sealed record DatosLiquidacion(
        IReadOnlyList<Gasto> Gastos,
        TipoMoneda Moneda,
        List<TransferenciaLiquidacion> Transferencias
    );
    // Método compartido para preparar liquidación con o sin persistencia
    private async Task<DatosLiquidacion> PrepararLiquidacionAsync(
        Guid viajeId,
        Guid usuarioActualId,
        LiquidarGastosRequestDto request,
        CancellationToken ct = default)
    {
        await ValidarMiembroAsync(viajeId, usuarioActualId, ct);

        var gastoIds = request.GastoIds?.ToArray()
            ?? throw new ArgumentException("Debe indicar los gastos.");

        if (gastoIds.Length == 0)
            throw new ArgumentException("Debe seleccionar al menos un gasto.");

        if (gastoIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("Los IDs de los gastos no son válidos.");

        if (gastoIds.Distinct().Count() != gastoIds.Length)
            throw new ArgumentException("No puede haber gastos duplicados.");

        var gastos = await _gastosRepository
            .ObtenerPendientesParaLiquidarAsync(
                viajeId,
                gastoIds,
                ct);

        // Detecta gastos inexistentes, de otro viaje
        // o que ya fueron liquidados.
        if (gastos.Count != gastoIds.Length)
        {
            throw new InvalidOperationException(
                "Uno o más gastos no existen, no pertenecen al viaje " +
                "o ya fueron liquidados.");
        }

        var monedas = gastos
            .Select(g => g.Moneda)
            .Distinct()
            .ToArray();

        if (monedas.Length != 1)
        {
            throw new InvalidOperationException(
                "No se pueden liquidar gastos de distintas monedas juntos.");
        }

        var moneda = monedas[0];

        var saldos = CalcularSaldosDeGastos(gastos);

        var transferencias = ConstruirTransferencias(saldos);

        return new DatosLiquidacion(
            gastos,
            moneda,
            transferencias
        );
    }
    private static GastoDto MapearGastoDto(Gasto gasto)
    {
        return new GastoDto
        {
            Id = gasto.Id,
            ViajeId = gasto.ViajeId,
            Concepto = gasto.Concepto,
            Monto = gasto.Monto,
            Moneda = gasto.Moneda,
            PagadoPorMiembroId = gasto.PagadoPorMiembroId,
            RegistradoPorMiembroId = gasto.RegistradoPorMiembroId,
            FechaGasto = gasto.FechaGasto,
            Divisiones = gasto.Divisiones.Select(d => MapearDivisionGastoDto(d)),
            LiquidacionId = gasto.LiquidacionId,
            Liquidado = gasto.LiquidacionId.HasValue
        };
    }
    private static DivisionGastoDto MapearDivisionGastoDto(DivisionGasto d)
    {
        return new DivisionGastoDto
        {
            UsuarioId = d.ParticipanteId,
            MontoAsignado = d.MontoAsignado
        };
    }

    private static IReadOnlyCollection<TransferenciaLiquidacionDto>
    MapearTransferenciasDto(
        IEnumerable<TransferenciaLiquidacion> transferencias)
    {
        return transferencias.Select(t =>
            new TransferenciaLiquidacionDto
            {
                DeParticipanteId = t.DeParticipanteId,
                AParticipanteId = t.AParticipanteId,
                Monto = t.Monto
            }).ToArray();
    }

    private static List<DivisionGasto> ConstruirDivisiones(
        Guid viajeId,
        Guid gastoId,
        decimal montoTotal,
        IReadOnlyCollection<Guid> participantesIds)
    {
        if (participantesIds.Count == 0)
            throw new ArgumentException("El gasto debe tener al menos un participante.");

        if (participantesIds.Distinct().Count() != participantesIds.Count)
        {
            throw new ArgumentException("No puede haber participantes duplicados.");
        }

        var resultado = new List<DivisionGasto>();

        
        // Dividir en partes iguales
        var totalCentavos = decimal.ToInt64(montoTotal * 100m);
        var cantidad = participantesIds.Count;

        var baseCentavos = totalCentavos / cantidad;
        var restoCentavos = totalCentavos % cantidad;

        // Orden estable para asignar los centavos sobrantes.
        var ordenados = participantesIds.OrderBy(id => id).ToList();

        for (var i = 0; i < ordenados.Count; i++)
        {
            var centavos = baseCentavos + (i < restoCentavos ? 1 : 0);

            resultado.Add(new DivisionGasto
            {
                Id = Guid.NewGuid(),
                GastoId = gastoId,
                ViajeId = viajeId,
                ParticipanteId = ordenados[i],
                MontoAsignado = centavos / 100m
            });
        }

        return resultado;
    }

    private static Dictionary<Guid, decimal> CalcularSaldosDeGastos(
        IEnumerable<Gasto> gastos)
    {
        var saldos = new Dictionary<Guid, decimal>();

        void AgregarSaldo(Guid usuarioId, decimal importe)
        {
            saldos[usuarioId] = saldos.GetValueOrDefault(usuarioId) + importe;
        }

        foreach (var gasto in gastos)
        {
            // Quien pagó tiene un saldo positivo.
            AgregarSaldo(
                gasto.PagadoPorMiembroId,
                gasto.Monto);

            // A cada participante se le descuenta su parte.
            foreach (var division in gasto.Divisiones)
            {
                AgregarSaldo(
                    division.ParticipanteId,
                    -division.MontoAsignado);
            }
        }

        // La suma debería ser cero porque las divisiones
        // de cada gasto deben sumar exactamente su importe total.
        if (saldos.Values.Sum() != 0m)
        {
            throw new InvalidOperationException("Los importes de las divisiones no coinciden con los gastos.");
        }

        return saldos;
    }

    private static List<TransferenciaLiquidacion> ConstruirTransferencias(
        Dictionary<Guid, decimal> saldos)
    {
        var deudores = saldos
            .Where(x => x.Value < 0)
            .Select(x => (
                UsuarioId: x.Key,
                Restante: -x.Value))
            .OrderBy(x => x.UsuarioId)
            .ToList();

        var acreedores = saldos
            .Where(x => x.Value > 0)
            .Select(x => (
                UsuarioId: x.Key,
                Restante: x.Value))
            .OrderBy(x => x.UsuarioId)
            .ToList();

        var transferencias = new List<TransferenciaLiquidacion>();

        var i = 0;
        var j = 0;

        while (i < deudores.Count && j < acreedores.Count)
        {
            var deudor = deudores[i];
            var acreedor = acreedores[j];

            var monto = Math.Min(
                deudor.Restante,
                acreedor.Restante);

            transferencias.Add(new TransferenciaLiquidacion
            {
                Id = Guid.NewGuid(),
                DeParticipanteId = deudor.UsuarioId,
                AParticipanteId = acreedor.UsuarioId,
                Monto = monto
            });

            deudores[i] = (
                deudor.UsuarioId,
                deudor.Restante - monto);

            acreedores[j] = (
                acreedor.UsuarioId,
                acreedor.Restante - monto);

            if (deudores[i].Restante == 0)
                i++;

            if (acreedores[j].Restante == 0)
                j++;
        }

        if (i != deudores.Count || j != acreedores.Count)
        {
            throw new InvalidOperationException("No fue posible compensar todos los saldos.");
        }

        return transferencias;
    }

    // --------------
    // Validaciones
    // --------------
    private static void ValidarDatosBasicos(
        string concepto,
        decimal monto,
        TipoMoneda moneda,
        DateTime fechaGasto)
    {
        if (string.IsNullOrWhiteSpace(concepto))
            throw new ArgumentException("El concepto es obligatorio.");

        if (monto <= 0 || monto * 100m != decimal.Truncate(monto * 100m))
        {
            throw new ArgumentException("El importe debe ser positivo y tener como máximo dos decimales.");
        }

        if (!Enum.IsDefined<TipoMoneda>(moneda))
        {
            throw new ArgumentException($"La moneda debe ser un código valido. {Enum.GetValues<TipoMoneda>()}");
        }

        // validaciones de fecha
    }
    private async Task ValidarMiembrosAsync(
        Guid viajeId,
        Guid usuarioActualId,
        Guid pagadoPorUsuarioId,
        IReadOnlyCollection<Guid> participantesIds,
        CancellationToken ct)
    {
        var idsNecesarios = participantesIds
            .Append(usuarioActualId)
            .Append(pagadoPorUsuarioId)
            .Distinct()
            .ToArray();

        var existentes = await _gastosRepository
            .ObtenerMiembrosExistentesAsync(viajeId, idsNecesarios, ct);

        var faltantes = idsNecesarios
            .Where(id => !existentes.Contains(id))
            .ToArray();

        if (faltantes.Length > 0)
        {
            throw new ArgumentException("Todos los usuarios involucrados deben ser miembros del viaje.");
        }
    }
    private async Task ValidarMiembroAsync(
        Guid viajeId,
        Guid usuarioId,
        CancellationToken ct)
    {
        var miembros = await _gastosRepository
            .ObtenerMiembrosExistentesAsync(
                viajeId,
                new[] { usuarioId },
                ct);

        if (!miembros.Contains(usuarioId))
            throw new UnauthorizedAccessException("El usuario no pertenece a este viaje.");
    }

    private static void ValidarPuedeModificar(
        Gasto gasto,
        Guid usuarioActualId)
    {
        if (gasto.RegistradoPorMiembroId != usuarioActualId)
            throw new UnauthorizedAccessException("No tenés permiso para modificar este gasto.");
    }
}