using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;

namespace Play.Catalog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemsController : ControllerBase {
    
    private static readonly List<ItemDto> items = new() {
        new ItemDto(Guid.NewGuid(), "Potion", "Restore a little amount of HP", 5, DateTimeOffset.UtcNow),
        new ItemDto(Guid.NewGuid(), "Antidote", "Cures poison", 7, DateTimeOffset.UtcNow),
        new ItemDto(Guid.NewGuid(), "Bronze sword", "Deals a small amount of damage", 20, DateTimeOffset.UtcNow),
        new ItemDto(Guid.NewGuid(), "Bronze sword", "Deals a small amount of damage", 20, DateTimeOffset.UtcNow),
        new ItemDto(Guid.NewGuid(), "Bronze sword", "Deals a small amount of damage", 20, DateTimeOffset.UtcNow)
    };

    [HttpGet]
    public List<ItemDto> Get() => items;


    [HttpGet("{id}")]
    public ItemDto? GetById(Guid id) => items.FirstOrDefault(item => item.Id == id);

    [HttpPost]
    public ActionResult<ItemDto> Post(CreateItemDto item) {
        ItemDto newItem = new ItemDto(Guid.NewGuid(), item.Name, item.Description, item.Price, DateTimeOffset.UtcNow);
        items.Add(newItem);
        return new CreatedAtActionResult(nameof(GetById), "Items", new { id = newItem.Id }, newItem);
        // return CreatedAtAction(nameof(GetById), new {id = newItem.Id}, newItem);
    }

    [HttpPut("{id}")]
    public IActionResult Put(Guid id, UpdateItemDto updatingItem) {
        ItemDto existingItem = items.FirstOrDefault(item => item.Id == id);

        if (existingItem != null) {
            ItemDto updatedItem = existingItem with {
                Name = updatingItem.Name,
                Description = updatingItem.Description,
                Price = updatingItem.Price
            };
            int index = items.FindIndex(item => item.Id == id);
            items[index] = updatedItem;
            return NoContent();
        } else return NotFound();
        // return CreatedAtAction(nameof(GetById), new {id = newItem.Id}, newItem);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id) {
        int index = items.FindIndex(item => item.Id == id);
        items.RemoveAt(index);
        return NoContent();
    }
}