﻿using ShareBook.Domain;
using ShareBook.Repository.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShareBook.Service
{
    public class BookUserEmailService : IBookUsersEmailService
    {
        private const string BookRequestedTemplate = "BookRequestedTemplate";
        private const string BookNoticeDonorTemplate = "BookNoticeDonorTemplate";
        private const string BookDonatedTemplate = "BookDonatedTemplate";
        private const string BookDonatedTemplateNotifyDonor = "BookDonatedNotifyDonorTemplate";
        private const string BookNoticeDeclinedUsersTemplate = "BookNoticeDeclinedUsersTemplate";
        private const string BookCanceledNoticeUsersTemplate = "BookCanceledNoticeUsersTemplate";
        private const string BookTrackingNumberNoticeWinnerTemplate = "BookTrackingNumberNoticeWinnerTemplate";
        private const string BookDonatedTitle = "Parabéns você foi selecionado!";
        private const string BookDonatedTitleNotifyDonor = "Parabéns você escolheu um ganhador!";
        private const string BookRequestedTitle = "Um livro foi solicitado - Sharebook";
        private const string BookNoticeDonorTitle = "Seu livro foi solicitado - Sharebook";
        private const string BookCanceledTemplate = "BookCanceledTemplate";
        private const string BookCanceledTitle = "Livro cancelado - Sharebook";
        private const string BookTrackingNumberNoticeWinnerTitle = "Seu livro foi postado - Sharebook";
        private const string BookNoticeInterestedTemplate = "BookNoticeInterestedTemplate";
        private const string BookNoticeInterestedTitle = "Sharebook - Você solicitou um livro";

        private readonly IUserService _userService;
        private readonly IBookService _bookService;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplate _emailTemplate;

        public BookUserEmailService(IUserService userService, IBookService bookService, IEmailService emailService, IEmailTemplate emailTemplate)
        {
            _userService = userService;
            _bookService = bookService;
            _emailService = emailService;
            _emailTemplate = emailTemplate;
        }

        public async Task SendEmailBookDonated(BookUser bookUser)
        {
            var bookDonated = bookUser.Book;
            if (bookDonated.User == null)
                bookDonated.User = _userService.Find(bookUser.Book.UserId);
            var vm = new
            {
                Book = bookDonated,
                bookUser.User
            };
            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookDonatedTemplate, vm);
            await _emailService.Send(bookUser.User.Email, bookUser.User.Name, html, BookDonatedTitle, true);
        }

        public async Task SendEmailBookDonatedNotifyDonor(Book book, User winner)
        {
            var vm = new
            {
                BookTitle = book.Title,
                DonorName = book.User.Name,
                Facilitator = book.UserFacilitator,
                Winner = winner
            };
            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookDonatedTemplateNotifyDonor, vm);

            // TODO: não enviar cópia para admins quando esse processo estiver bem amadurecido.
            var copyAdmins = true;
            await _emailService.Send(book.User.Email, book.User.Name, html, BookDonatedTitleNotifyDonor, copyAdmins);
        }

        public async Task SendEmailBookRequested(BookUser bookUser)
        {
            var includeList = new IncludeList<Book>(x => x.User);
            var bookRequested = _bookService.Find(includeList, bookUser.BookId);

            var requestingUser = _userService.Find(bookUser.UserId);

            var vm = new
            {
                Request = bookUser,
                Book = bookRequested,
                RequestingUser = requestingUser,
            };
            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookRequestedTemplate, vm);
            await _emailService.SendToAdmins(html, BookRequestedTitle);
        }

        public async Task SendEmailBookDonor(BookUser bookUser, Book bookRequested)
        {
            //obter o endereço do interessado
            var donatedUser = this._userService.Find(bookUser.UserId);
            var vm = new
            {

                Request = bookUser,
                Book = bookRequested,
                DonatedLocation = GenerateDonatedLocation(donatedUser),
                Donor = new
                {
                    Name = bookRequested.User.Name,
                    ChooseDate = string.Format("{0:dd/MM/yyyy}", bookRequested.ChooseDate.Value)
                },
                RequestingUser = new { bookUser.NickName },

            };

            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookNoticeDonorTemplate, vm);

            await _emailService.Send(bookRequested.User.Email, bookRequested.User.Name, html, BookNoticeDonorTitle);

        }

        public async Task SendEmailBookInterested(BookUser bookUser, Book book)
        {
            var vm = new
            {
                NameBook = bookUser.Book.Title,
                NameFacilitator = book.UserFacilitator.Name,
                LinkedinFacilitator = book.UserFacilitator.Linkedin,
                PhoneFacilitator = book.UserFacilitator.Phone,
                EmailFacilitator = book.UserFacilitator.Email,
                ChooseDate = string.Format("{0:dd/MM/yyyy}", book.ChooseDate.Value) ,
                NameInterested = bookUser.User.Name,
            };

            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookNoticeInterestedTemplate, vm);
            await _emailService.Send(bookUser.User.Email, bookUser.User.Name, html, BookNoticeInterestedTitle);
        }

        /// <summary>
        /// Metodo tem como finalizado fazer tratativas na geração da informação de localidade que será enviado doador
        /// </summary>
        /// <param name="donatedUser"></param>
        /// <returns></returns>
        private string GenerateDonatedLocation(User donatedUser)
        {
            string ND = "N/D";
            if (donatedUser == null) return ND;

            if (donatedUser.Address == null) return ND;


            var address = donatedUser.Address;
            string location = string.Empty;

            if (!string.IsNullOrEmpty(address.City))
                location = address.City.ToUpper();

            if (!string.IsNullOrEmpty(address.State))
                location += $"/{address.State}";            

            return location;

        }

        public async Task SendEmailDonationDeclined(Book book, BookUser bookUserWinner, List<BookUser> bookUsersDeclined)
        {
            var vm = new
            {
                BookTitle = book.Title,
                BookWinner = bookUserWinner.User.Name
            };

            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookNoticeDeclinedUsersTemplate, vm);


            bookUsersDeclined.ForEach(bookUser =>
            {
                _emailService.Send(bookUser.User.Email, bookUser.User.Name, html, $"SHAREBOOK - GANHADOR DO LIVRO {book.Title.ToUpper()}").Wait();
            });

        }

        public async Task SendEmailDonationCanceled(Book book, List<BookUser> bookUsers){
            var vm = new {
                book
            };

            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookCanceledNoticeUsersTemplate, vm);

            bookUsers.ForEach(bookUser => {
                _emailService.Send(bookUser.User.Email, bookUser.User.Name, html,  $"SHAREBOOK - DOAÇÃO CANCELADA").Wait();
            });
            
        }

        public async Task SendEmailBookCanceledToAdmins(Book book)
        {
            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookCanceledTemplate, book);
            await _emailService.SendToAdmins(html, BookCanceledTitle);
        }
    
        public async Task SendEmailTrackingNumberInformed(BookUser bookUserWinner, Book book)
        {
            var vm = new
            {
                book = book,
                NameFacilitator = book.UserFacilitator.Name,
                LinkedInFacilitator = book.UserFacilitator.Linkedin,
                ZapFacilitator = book.UserFacilitator.Phone,
                EmailFacilitator = book.UserFacilitator.Email,
            };
            var html = await _emailTemplate.GenerateHtmlFromTemplateAsync(BookTrackingNumberNoticeWinnerTemplate, vm);
            await _emailService.Send(bookUserWinner.User.Email, bookUserWinner.User.Name, html, BookTrackingNumberNoticeWinnerTitle, false);
        }
    
    
    }
}
