﻿using Grand.Domain.News;
using Grand.Domain.Seo;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallNews()
    {
        var defaultLanguage = _languageRepository.Table.FirstOrDefault();
        var news = new List<NewsItem> {
            new() {
                AllowComments = false,
                Title = "About Grandnode",
                Short =
                    "It's stable and highly usable. From downloads to documentation, www.grandnode.com offers a comprehensive base of information, resources, and support to the grandnode community.",
                Full =
                    "<p>For full feature list go to <a href=\"https://grandnode.com\">grandnode.com</a></p><p>Providing outstanding custom search engine optimization, web development services and e-commerce development solutions to our clients at a fair price in a professional manner.</p>",
                Published = true
            },
            new() {
                AllowComments = false,
                Title = "Grandnode new release!",
                Short =
                    "grandnode includes everything you need to begin your e-commerce online store. We have thought of everything and it's all included! grandnode is a fully customizable shopping cart",
                Full =
                    "<p>Grandnode includes everything you need to begin your e-commerce online store. We have thought of everything and it's all included!</p>",
                Published = true
            },
            new() {
                AllowComments = false,
                Title = "New online store is open!",
                Short =
                    "The new grandnode store is open now! We are very excited to offer our new range of products. We will be constantly adding to our range so please register on our site.",
                Full =
                    "<p>Our online store is officially up and running. Stock up for the holiday season! We have a great selection of items. We will be constantly adding to our range so please register on our site, this will enable you to keep up to date with any new products.</p><p>All shipping is worldwide and will leave the same day an order is placed! Happy Shopping and spread the word!!</p>",
                Published = true
            }
        };
        news.ForEach(x => _newsItemRepository.Insert(x));

        //search engine names
        foreach (var newsItem in news)
        {
            newsItem.SeName = SeoExtensions.GenerateSlug(newsItem.Title, false, false, false);
            await _entityUrlRepository.InsertAsync(new EntityUrl {
                EntityId = newsItem.Id,
                EntityName = "NewsItem",
                LanguageId = "",
                IsActive = true,
                Slug = newsItem.SeName
            });
            await _newsItemRepository.UpdateAsync(newsItem);
        }
    }
}