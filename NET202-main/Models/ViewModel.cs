public class MenuViewModel
{
    public IEnumerable<ProductViewModel> BestSellers { get; set; }
    public IEnumerable<ProductViewModel> AllProducts { get; set; }
}

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }    
    public string CategoryName { get; set; }
}