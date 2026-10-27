using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;

namespace VetClinic.API.Hateoas;

/// <summary>Gera os links HATEOAS com Url.Link (rotas nomeadas), sem biblioteca externa.</summary>
public static class HateoasHelper
{
    public static TutorResponse WithLinks(this TutorResponse tutor, IUrlHelper url)
    {
        var self = url.Link(RouteNames.GetTutorById, new { id = tutor.Id })!;
        tutor.Links = new List<LinkDto>
        {
            new(self, "self", "GET"),
            new(self, "update", "PUT"),
            new(self, "delete", "DELETE"),
            new(url.Link(RouteNames.GetPets, new { tutorId = tutor.Id })!, "pets", "GET")
        };
        return tutor;
    }

    public static PetResponse WithLinks(this PetResponse pet, IUrlHelper url)
    {
        var self = url.Link(RouteNames.GetPetById, new { id = pet.Id })!;
        pet.Links = new List<LinkDto>
        {
            new(self, "self", "GET"),
            new(self, "update", "PUT"),
            new(self, "delete", "DELETE"),
            new(url.Link(RouteNames.GetTutorById, new { id = pet.TutorId })!, "tutor", "GET")
        };
        return pet;
    }

    /// <summary>Links de navegação da página: self, first, last, previous, next.</summary>
    public static PageResult<T> WithPageLinks<T>(this PageResult<T> page, IUrlHelper url, string routeName, object? extraValues = null)
    {
        string PageLink(int number)
        {
            var values = new RouteValueDictionary(extraValues)
            {
                ["pageNumber"] = number,
                ["pageSize"] = page.PageSize
            };
            return url.Link(routeName, values)!;
        }

        var lastPage = Math.Max(page.TotalPages, 1);
        page.Links = new List<LinkDto>
        {
            new(PageLink(page.PageNumber), "self", "GET"),
            new(PageLink(1), "first", "GET"),
            new(PageLink(lastPage), "last", "GET")
        };
        if (page.HasPrevious)
            page.Links.Add(new LinkDto(PageLink(page.PageNumber - 1), "previous", "GET"));
        if (page.HasNext)
            page.Links.Add(new LinkDto(PageLink(page.PageNumber + 1), "next", "GET"));

        return page;
    }
}

public static class RouteNames
{
    public const string GetTutores = "GetTutores";
    public const string GetTutorById = "GetTutorById";
    public const string GetPets = "GetPets";
    public const string GetPetById = "GetPetById";
}
