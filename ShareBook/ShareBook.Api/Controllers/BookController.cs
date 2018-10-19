﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ShareBook.Api.Filters;
using ShareBook.Api.ViewModels;
using ShareBook.Domain;
using ShareBook.Domain.Common;
using ShareBook.Service;
using ShareBook.Service.Authorization;
using ShareBook.Repository.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace ShareBook.Api.Controllers
{
    [Route("api/[controller]")]
    [GetClaimsFilter]
    [EnableCors("AllowAllHeaders")]
    public class BookController : Controller
    {
        private readonly IBookUserService _bookUserService;
        private readonly IBookService _service;
        private readonly IMapper _autoMapper;

        private Expression<Func<Book, object>> _defaultOrder = x => x.Id;

        public BookController(IMapper mapper, IBookService bookService, IBookUserService bookUserService)
        {
            _autoMapper = mapper;
            _service = bookService;
            _bookUserService = bookUserService;
        }

        protected void SetDefault(Expression<Func<Book, object>> defaultOrder)
        {
            _defaultOrder = defaultOrder;
        }

        [HttpGet()]
        [Authorize("Bearer")]
        [AuthorizationFilter(Permissions.Permission.DonateBook)]
        public PagedList<BooksVM> GetAll() => Paged(1, 15);

        [HttpGet("{page}/{items}")]
        [Authorize("Bearer")]
        [AuthorizationFilter(Permissions.Permission.DonateBook)]
        public PagedList<BooksVM> Paged(int page, int items)
        {
            var books = _service.Get(x => x.Title, page, items, new IncludeList<Book>(x => x.User, x => x.BookUsers));
            var responseVM = Mapper.Map<List<BooksVM>>(books.Items);
            return new PagedList<BooksVM>()
            {
                Page = page,
                TotalItems = books.TotalItems,
                ItemsPerPage = items,
                Items = responseVM
            };
        }

        [HttpGet("{id}")]
        public Book GetById(string id) => _service.Find(new Guid(id));

        [Authorize("Bearer")]
        [HttpPost("Approve/{id}")]
        [AuthorizationFilter(Permissions.Permission.ApproveBook)]
        public Result<Book> Approve(string id) => _service.Approve(new Guid(id));

        [HttpGet("FreightOptions")]
        public IList<dynamic> FreightOptions()
        {
            var freightOptions = _service.FreightOptions();
            return freightOptions;
        }

        [Authorize("Bearer")]
        [HttpGet("GranteeUsersByBookId/{bookId}")]
        [AuthorizationFilter(Permissions.Permission.DonateBook)]
        public IList<User> GetGranteeUsersByBookId(string bookId) => _bookUserService.GetGranteeUsersByBookId(new Guid(bookId));

        [HttpGet("Slug/{slug}")]
        public IActionResult Get(string slug)
        {
            var book = _service.BySlug(slug);
            return book != null ? (IActionResult)Ok(book) : NotFound();
        }

        [HttpGet("Top15NewBooks")]
        public IActionResult Top15NewBooks()
        {
            try
            {
                var top15NewBooksVM = _autoMapper.Map<List<Top15NewBooksVM>>(_service.Top15NewBooks().Items);
                return new OkObjectResult(top15NewBooksVM);
            }

            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }
         }

        [HttpGet("Random15Books")]
        public IList<Book> Random15Books() => _service.Random15Books();

        [Authorize("Bearer")]
        [HttpGet("Title/{title}/{page}/{items}")]
        public PagedList<Book> ByTitle(string title, int page, int items) => _service.ByTitle(title, page, items);

        [Authorize("Bearer")]
        [HttpGet("Author/{author}/{page}/{items}")]
        public PagedList<Book> ByAuthor(string author, int page, int items) => _service.ByAuthor(author, page, items);

        [HttpGet("FullSearch/{criteria}/{page}/{items}")]
        public PagedList<Book> FullSearch(string criteria, int page, int items) => _service.FullSearch(criteria, page, items);

        [Authorize("Bearer")]
        [HttpGet("FullSearchAdmin/{criteria}")]
        [AuthorizationFilter(Permissions.Permission.DonateBook)]
        public PagedList<Book> FullSearchAdmin(string criteria, int page, int items)
        {
            var isAdmin = true;
            return _service.FullSearch(criteria, page, items, isAdmin);
        }

        [Authorize("Bearer")]
        [ProducesResponseType(typeof(Result), 200)]
        [HttpPost("Request")]
        public IActionResult RequestBook([FromBody] RequestBookVM requestBookVM)
        {
            _bookUserService.Insert(requestBookVM.BookId, requestBookVM.Reason);
            var result = new Result
            {
                SuccessMessage = "Pedido realizo com sucesso!",
            };
            return Ok(result);
        }

        [Authorize("Bearer")]
        [HttpPost]
        public Result<Book> Create([FromBody] CreateBookVM createBookVM)
        {
            var book = Mapper.Map<Book>(createBookVM);
            return _service.Insert(book);
        }

        [Authorize("Bearer")]
        [HttpPut("{id}")]
        [AuthorizationFilter(Permissions.Permission.ApproveBook)]
        public Result<Book> Update(Guid id, [FromBody] UpdateBookVM updateBookVM)
        {
            updateBookVM.Id = id;
            var book = Mapper.Map<Book>(updateBookVM);

            return _service.Update(book);
        }

        [Authorize("Bearer")]
        [HttpPut("Donate/{bookId}")]
        [ProducesResponseType(typeof(Result), 200)]
        [AuthorizationFilter(Permissions.Permission.DonateBook)]
        public IActionResult DonateBook(Guid bookId, [FromBody] DonateBookUserVM donateBookUserVM)
        {
            _bookUserService.DonateBook(bookId, donateBookUserVM.UserId, donateBookUserVM.Note);

            var result = new Result
            {
                SuccessMessage = "Livro doado com sucesso!",
            };

            return Ok(result);
        }

        [Authorize("Bearer")]
        [HttpDelete("{id}")]
        [AuthorizationFilter(Permissions.Permission.DonateBook)]
        public Result Delete(Guid id) => _service.Delete(id);

        [Authorize("Bearer")]
        [HttpGet("Requested/{bookId}")]
        public Result Requested(Guid bookId)
        {
            var result = new Result
            {
                Value = new { bookRequested = _service.UserRequestedBook(bookId) },
            };

            return result;
        }

        [Authorize("Bearer")]
        [HttpGet("MyRequests/{page}/{items}")]
        public PagedList<MyBookRequestVM> MyRequests(int page, int items)
        {
            var donation = _bookUserService.GetRequestsByUser();
            var myBooksRequestsVM = Mapper.Map<List<MyBookRequestVM>>(donation.Items);

            return new PagedList<MyBookRequestVM>()
            {
                Page = page,
                TotalItems = donation.TotalItems,
                ItemsPerPage = items,
                Items = myBooksRequestsVM
            };
        }

        [Authorize("Bearer")]
        [HttpGet("MyDonations")]
        public IList<BooksVM> MyDonations()
        {
            Guid userId = new Guid(Thread.CurrentPrincipal?.Identity?.Name);
            var donations = _service.GetUserDonations(userId);
            return Mapper.Map<List<BooksVM>>(donations);
        }
    }
}
