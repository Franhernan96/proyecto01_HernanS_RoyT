namespace MeteoAula.Pruebas
{
    /// <summary>Un caso de prueba con su resultado.</summary>
    public class CasoPrueba
    {
        public string Id { get; set; }
        public string Descripcion { get; set; }
        public string Entrada { get; set; }
        public string Esperado { get; set; }
        public string Obtenido { get; set; }
        public string Resultado { get; set; }   // APROBADO, FALLIDO o MANUAL

        public CasoPrueba(string id, string descripcion, string entrada, string esperado)
        {
            Id = id;
            Descripcion = descripcion;
            Entrada = entrada;
            Esperado = esperado;
            Obtenido = "";
            Resultado = "";
        }
    }
}
