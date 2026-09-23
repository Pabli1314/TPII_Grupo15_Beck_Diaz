using Datos;
using Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logica
{
    /// <summary>
    /// Todavía no existe una tabla "Tarifa" en la base de datos: se administra en memoria
    /// (estática, para que sobreviva mientras la app esté abierta), sembrada a partir de los
    /// tipos de habitación reales. El día que exista TarifaDAO, esta clase pasa a delegarle
    /// las operaciones sin cambiar su forma pública.
    /// </summary>
    public class GestionTarifas
    {
        private static readonly List<Tarifa> _tarifas = new();
        private static bool _inicializado;
        private static int _siguienteId = 1;

        public List<Tarifa> ObtenerTarifas()
        {
            AsegurarSembrado();
            return _tarifas.OrderBy(t => t.TipoHabitacionNombre).ToList();
        }

        public void CrearTarifa(Tarifa tarifa)
        {
            AsegurarSembrado();
            Validar(tarifa);

            tarifa.IdTarifa = _siguienteId++;
            tarifa.UltimaModificacion = DateTime.Now;
            _tarifas.Add(tarifa);
        }

        public void EditarTarifa(Tarifa tarifa)
        {
            AsegurarSembrado();
            Validar(tarifa);

            Tarifa existente = _tarifas.FirstOrDefault(t => t.IdTarifa == tarifa.IdTarifa)
                ?? throw new InvalidOperationException("No se encontró la tarifa a editar.");

            existente.IdTipoHabitacion = tarifa.IdTipoHabitacion;
            existente.TipoHabitacionNombre = tarifa.TipoHabitacionNombre;
            existente.PrecioPorHora = tarifa.PrecioPorHora;
            existente.PrecioPorFraccion = tarifa.PrecioPorFraccion;
            existente.PrecioAdicional = tarifa.PrecioAdicional;
            existente.Activa = tarifa.Activa;
            existente.UltimaModificacion = DateTime.Now;
        }

        public void CambiarEstado(int idTarifa, bool activa)
        {
            AsegurarSembrado();
            Tarifa existente = _tarifas.FirstOrDefault(t => t.IdTarifa == idTarifa)
                ?? throw new InvalidOperationException("No se encontró la tarifa.");

            existente.Activa = activa;
            existente.UltimaModificacion = DateTime.Now;
        }

        private static void Validar(Tarifa tarifa)
        {
            if (tarifa.IdTipoHabitacion <= 0)
            {
                throw new ArgumentException("Debe seleccionar un tipo de habitación.");
            }

            if (tarifa.PrecioPorHora < 0 || tarifa.PrecioPorFraccion < 0 || tarifa.PrecioAdicional < 0)
            {
                throw new ArgumentException("Los precios no pueden ser negativos.");
            }
        }

        private static void AsegurarSembrado()
        {
            if (_inicializado)
            {
                return;
            }

            foreach (TipoHabitacion tipo in TipoHabitacionDAO.ObtenerTodos())
            {
                _tarifas.Add(new Tarifa
                {
                    IdTarifa = _siguienteId++,
                    IdTipoHabitacion = tipo.IdTipoHabitacion,
                    TipoHabitacionNombre = tipo.Descripcion,
                    PrecioPorHora = 0,
                    PrecioPorFraccion = 0,
                    PrecioAdicional = 0,
                    Activa = true,
                    UltimaModificacion = DateTime.Now
                });
            }

            _inicializado = true;
        }
    }
}
