using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Catalog.Service.Extensions;
using Play.Catalog.Service.Repositories;

namespace Play.Catalog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController : ControllerBase {
    
    private readonly IItemsRepository itemsRepo;

    public ItemsController(IItemsRepository repository)
    {
        itemsRepo = repository;
    }

    [HttpGet]
    public async Task<IEnumerable<ItemDto>> GetAsync() => (await itemsRepo.GetAllAsync()).Select(item => item.AsDto());

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id) {
        ItemDto? item = (await itemsRepo.GetAsync(id))?.AsDto();
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto) {

        Item newItem = new Item {
            Name = createItemDto.Name, 
            Description = createItemDto.Description,
            Price = createItemDto.Price,
            CreatedDate = DateTimeOffset.UtcNow
        };
        await itemsRepo.CreateAsync(newItem);
        return CreatedAtAction(nameof(GetByIdAsync), new {id = newItem.Id}, newItem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updatingItem) {
        Item? existingItem = await itemsRepo.GetAsync(id);
        if (existingItem == null) return NotFound(); 
        
        existingItem.Name = updatingItem.Name;
        existingItem.Description = updatingItem.Description;
        existingItem.Price = updatingItem.Price;
        
        await itemsRepo.UpdateAsync(existingItem);
        return NoContent();       
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id) {
        Item? existingItem = await itemsRepo.GetAsync(id);
        if (existingItem == null) return NotFound();
        await itemsRepo.DelteAsync(id);
        return NoContent();
    }
}