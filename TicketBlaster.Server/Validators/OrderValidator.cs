using FluentValidation;
using TicketBlaster.Shared.Models;
using System.Text.RegularExpressions;

namespace TicketBlaster.Server.Validators
{
    public class OrderValidator : AbstractValidator<Order>
    {
        public OrderValidator()
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Customer name is required")
                .Length(2, 200).WithMessage("Customer name must be between 2 and 200 characters")
                .Must(BeValidPersonName).WithMessage("Customer name contains invalid characters");

            RuleFor(x => x.CustomerEmail)
                .NotEmpty().WithMessage("Customer email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .Length(5, 256).WithMessage("Email must be between 5 and 256 characters");

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage("Customer phone is required")
                .Must(BeValidPhoneNumber).WithMessage("Invalid phone number format");

            RuleFor(x => x.BillingAddress)
                .NotEmpty().WithMessage("Billing address is required")
                .Length(5, 500).WithMessage("Billing address must be between 5 and 500 characters")
                .Must(BeValidAddress).WithMessage("Billing address contains invalid characters");

            RuleFor(x => x.BillingCity)
                .NotEmpty().WithMessage("Billing city is required")
                .Length(2, 100).WithMessage("City must be between 2 and 100 characters")
                .Must(BeValidCityName).WithMessage("City contains invalid characters");

            RuleFor(x => x.BillingState)
                .NotEmpty().WithMessage("Billing state is required")
                .Length(2, 100).WithMessage("State must be between 2 and 100 characters")
                .Must(BeValidStateName).WithMessage("State contains invalid characters");

            RuleFor(x => x.BillingZipCode)
                .NotEmpty().WithMessage("Billing zip code is required")
                .Matches(@"^[A-Z0-9\s\-]+$", RegexOptions.IgnoreCase)
                .WithMessage("Invalid zip code format");

            RuleFor(x => x.BillingCountry)
                .NotEmpty().WithMessage("Billing country is required")
                .Length(2, 100).WithMessage("Country must be between 2 and 100 characters")
                .Must(BeValidCountryName).WithMessage("Country contains invalid characters");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
                .Must(BeValidNotes).When(x => !string.IsNullOrEmpty(x.Notes))
                .WithMessage("Notes contain invalid characters");

            RuleFor(x => x.SubTotal)
                .GreaterThan(0).WithMessage("Subtotal must be greater than 0")
                .LessThanOrEqualTo(100000).WithMessage("Subtotal cannot exceed $100,000");

            RuleFor(x => x.TaxAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Tax amount cannot be negative")
                .LessThanOrEqualTo(x => x.SubTotal).WithMessage("Tax amount cannot exceed subtotal");

            RuleFor(x => x.ServiceFee)
                .GreaterThanOrEqualTo(0).WithMessage("Service fee cannot be negative")
                .LessThanOrEqualTo(x => x.SubTotal * 0.3m).WithMessage("Service fee cannot exceed 30% of subtotal");

            RuleFor(x => x.TotalAmount)
                .GreaterThan(0).WithMessage("Total amount must be greater than 0")
                .Equal(x => x.SubTotal + x.TaxAmount + x.ServiceFee)
                .WithMessage("Total amount calculation is incorrect");
        }

        private bool BeValidPersonName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            
            // Allow letters, spaces, apostrophes, hyphens, and periods
            return Regex.IsMatch(name, @"^[a-zA-Z\s'\-\.]+$");
        }

        private bool BeValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            
            // Remove common formatting characters
            var cleaned = Regex.Replace(phone, @"[\s\-\(\)\+\.]", "");
            
            // Check if it's a valid phone number (6-15 digits)
            return Regex.IsMatch(cleaned, @"^\d{6,15}$");
        }

        private bool BeValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;
            
            // Prevent injection attempts
            var dangerousPatterns = new[] { "<", ">", "script", "javascript:", "../", "..\\", "%2e%2e" };
            var lowerAddress = address.ToLower();
            
            foreach (var pattern in dangerousPatterns)
            {
                if (lowerAddress.Contains(pattern))
                    return false;
            }
            
            // Allow alphanumeric, spaces, and common address characters
            return Regex.IsMatch(address, @"^[a-zA-Z0-9\s\-\.,#/]+$");
        }

        private bool BeValidCityName(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return false;
            
            // Allow letters, spaces, apostrophes, and hyphens
            return Regex.IsMatch(city, @"^[a-zA-Z\s'\-]+$");
        }

        private bool BeValidStateName(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return false;
            
            // Allow letters, spaces, and hyphens
            return Regex.IsMatch(state, @"^[a-zA-Z\s\-]+$");
        }

        private bool BeValidCountryName(string country)
        {
            if (string.IsNullOrWhiteSpace(country)) return false;
            
            // Allow letters, spaces, and hyphens
            return Regex.IsMatch(country, @"^[a-zA-Z\s\-]+$");
        }

        private bool BeValidNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return true;
            
            // Prevent injection attempts
            var dangerousPatterns = new[] 
            { 
                "<script", "javascript:", "onerror=", "onload=", 
                "DROP TABLE", "DELETE FROM", "../", "..\\" 
            };
            
            var lowerNotes = notes.ToLower();
            foreach (var pattern in dangerousPatterns)
            {
                if (lowerNotes.Contains(pattern.ToLower()))
                    return false;
            }
            
            return true;
        }
    }

    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.EventId)
                .GreaterThan(0).WithMessage("Valid event ID is required");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required")
                .Must(x => x.Count <= 20).WithMessage("Cannot order more than 20 different ticket types");

            RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());

            RuleFor(x => x.Customer).SetValidator(new CustomerInfoValidator());
            RuleFor(x => x.Billing).SetValidator(new BillingAddressValidator());
        }
    }

    public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
    {
        public OrderItemRequestValidator()
        {
            RuleFor(x => x.TicketTypeId)
                .GreaterThan(0).WithMessage("Valid ticket type ID is required");

            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 20).WithMessage("Quantity must be between 1 and 20");

            RuleFor(x => x.DiscountCode)
                .Matches(@"^[A-Z0-9\-]{3,20}$", RegexOptions.IgnoreCase)
                .When(x => !string.IsNullOrEmpty(x.DiscountCode))
                .WithMessage("Invalid discount code format");
        }
    }

    public class CustomerInfoValidator : AbstractValidator<CustomerInfo>
    {
        public CustomerInfoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .Length(1, 100).WithMessage("First name must be between 1 and 100 characters")
                .Matches(@"^[a-zA-Z\s'\-\.]+$").WithMessage("First name contains invalid characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .Length(1, 100).WithMessage("Last name must be between 1 and 100 characters")
                .Matches(@"^[a-zA-Z\s'\-\.]+$").WithMessage("Last name contains invalid characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .Length(5, 256).WithMessage("Email must be between 5 and 256 characters");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required")
                .Matches(@"^[\d\s\-\(\)\+\.]{6,20}$").WithMessage("Invalid phone format");
        }
    }

    public class BillingAddressValidator : AbstractValidator<BillingAddress>
    {
        public BillingAddressValidator()
        {
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required")
                .Length(5, 500).WithMessage("Address must be between 5 and 500 characters")
                .Matches(@"^[a-zA-Z0-9\s\-\.,#/]+$").WithMessage("Address contains invalid characters");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required")
                .Length(2, 100).WithMessage("City must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s'\-]+$").WithMessage("City contains invalid characters");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required")
                .Length(2, 100).WithMessage("State must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-]+$").WithMessage("State contains invalid characters");

            RuleFor(x => x.ZipCode)
                .NotEmpty().WithMessage("Zip code is required")
                .Matches(@"^[A-Z0-9\s\-]{3,10}$", RegexOptions.IgnoreCase)
                .WithMessage("Invalid zip code format");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .Length(2, 100).WithMessage("Country must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-]+$").WithMessage("Country contains invalid characters");
        }
    }
}