namespace Restaurantes.Application.Requests
{
    public class GetRestaurantesRequest
    {
        public string? Nome { get; set; }
        public string? Tipo { get; set; }
        public string? Cidade { get; set; }
        public int PageNumber { get; set; } = 1; // Página atual, padrão é 1
        public int PageSize { get; set; } = 10; // Itens por página, padrão 10
    }
}
