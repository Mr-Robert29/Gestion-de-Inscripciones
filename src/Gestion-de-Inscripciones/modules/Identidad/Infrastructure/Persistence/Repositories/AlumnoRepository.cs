using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using SistemaEducativo.Modulos.Identidad.Domain.Repositories;
using SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Contexts;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Repositories
{
    public class AlumnoRepository : IAlumnoRepository
    {
        private readonly IdentidadDbContext _context;

        public AlumnoRepository(IdentidadDbContext context)
        {
            _context = context;
        }

        public async Task<Alumno> ObtenerPorIdAsync(int idAlumno)
        {
            return await _context.Alumnos
                .Include(a => a.Domicilio)
                .Include(a => a.Responsables)
                .FirstOrDefaultAsync(a => a.IdAlumno == idAlumno);
        }

        public async Task<Alumno> ObtenerPorDocumentoAsync(string nroDocumento)
        {
            return await _context.Alumnos
                .Include(a => a.Domicilio)
                .Include(a => a.Responsables)
                .FirstOrDefaultAsync(a => a.NroDocumento == nroDocumento);
        }

        public async Task AgregarAsync(Alumno alumno)
        {
            await _context.Alumnos.AddAsync(alumno);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Alumno alumno)
        {
            _context.Alumnos.Update(alumno);
            await _context.SaveChangesAsync();
        }
    }
}
