namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities
{
    public class Domicilio
    {
        public int IdDomicilio { get; set; }
        public string Calle {  get; set; }
        public string Numero { get; set; }
        public string Piso { get; set; }
        public string Departamento { get; set; }
        public string Barrio { get; set; }
        public string CodigoPostal { get; set; }
        public decimal Latitud {  get; set; }
        public decimal Longitud { get; set; }
        protected Domicilio() { }
        public Domicilio(string calle, string numero, string barrio, string codigoPostal)
        {
            Calle = calle;
            Numero = numero;
            Barrio = barrio;
            CodigoPostal = codigoPostal;
        }
        public void ValidarDireccion()
        {
            if (string.IsNullOrWhiteSpace(Calle) || string.IsNullOrWhiteSpace(Numero))
            {
                throw new InvalidOperationException("La calle y el número son obligatorios.");
            }
        }
        public (decimal Latitud, decimal Longitud) ObtenerCoordenadas()
        {
            return (Latitud, Longitud);
        }
        public void RegistrarCoordenadas(decimal latitud, decimal longitud)
        {
            Latitud = latitud;
            Longitud = longitud;
        }
    }
}
