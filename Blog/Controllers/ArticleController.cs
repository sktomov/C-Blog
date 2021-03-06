﻿using Blog.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class ArticleController : Controller
    {
        // GET: Article
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }
        //
        //GET: Article/List
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                //Get articles from database
                var articles = database.Articles.Include(a => a.Author).Include(t => t.Tags).ToList();

                return View(articles);
            }
        }

        //
        //GET: Article/Details
        public ActionResult Details (int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                var article = database.Articles.Where(a => a.Id == id).Include(a => a.Author).Include(t => t.Tags).First();
                if(article == null)
                {
                    return HttpNotFound();
                }

                return View(article);
            }
        }

        //
        //GET: Article/Create
        
        [Authorize]
        public ActionResult Create()
        {
            using (var database = new BlogDbContext())
            {
                var model = new ArticleViewModel();
                model.Categories = database.Categories
                    .OrderBy(c => c.Name)
                    .ToList();

                return View(model);
            }
        }

        //
        //POST: Article/Create
        [HttpPost]
        [Authorize]
        public ActionResult Create(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    //Get author id
                    var authorId = database.Users.Where(u => u.UserName == this.User.Identity.Name)
                        .First().Id;
                    //Set articles author
                    var article = new Article(authorId, model.Title, model.Content, model.CategoryId);
                    article.AuthorId = authorId;
                    //Save articles in DB

                    this.SetArticleTags(article, model, database);
                    database.Articles.Add(article);
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }

            } 
            return View(model);
        }

        public ActionResult Edit(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var db = new BlogDbContext())
            {
                var article = db.Articles.Where(a => a.Id == id).First();
                if(article == null)
                {
                    return HttpNotFound();
                }

                var model = new ArticleViewModel();
                model.Id = article.Id;
                model.Title = article.Title;
                model.Content = article.Content;
                model.CategoryId = article.CategoryId;
                model.Categories = db.Categories.OrderBy(c => c.Name).ToList();

                model.Tags = string.Join(", ", article.Tags.Select(t => t.Name));

                return View(model);
            }
            
        }

        [HttpPost]
        public ActionResult Edit(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using(var db = new BlogDbContext())
                {
                    var article = db.Articles.FirstOrDefault(a => a.Id == model.Id);
                    article.Title = model.Title;
                    article.Content = model.Content;
                    article.CategoryId = model.CategoryId;
                    this.SetArticleTags(article, model, db);
                    db.Entry(article).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        private void SetArticleTags(Article article, ArticleViewModel model, BlogDbContext db)
        {
            var tagsString = model.Tags.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.ToLower()).Distinct();

            article.Tags.Clear();

            foreach (var tagString in tagsString)
            {
                Tag tag = db.Tags.FirstOrDefault(t => t.Name.Equals(tagString));

                if(tag == null)
                {
                    tag = new Tag() { Name = tagString };
                    db.Tags.Add(tag);
                }

                article.Tags.Add(tag);
            }

        }

        //Get Article/Delete
        public ActionResult Delete(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var db = new BlogDbContext())
            {
                var article = db.Articles.Where(a => a.Id == id).Include(a => a.Author).Include(a => a.Category).First();

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                ViewBag.TagsString = string.Join(", ", article.Tags.Select(t => t.Name));

                if (article == null)
                {
                    return HttpNotFound();
                }

                return View(article);
            }
            
        }
        
        [HttpPost]
        [ActionName ("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var db = new BlogDbContext())
            {
                var article = db.Articles.Where(a => a.Id == id).Include(a => a.Author).First();

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                if (article == null)
                {
                    return HttpNotFound();
                }

                db.Articles.Remove(article);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }
        private  bool IsUserAuthorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);

            return isAdmin || isAuthor;
        }
    }
}