using FluentValidation;
using TicketBlaster.Shared.Models;
using System;
using System.Text.RegularExpressions;

namespace TicketBlaster.Server.Validators
{
    public class EventValidator : AbstractValidator<Event>
    {
        public EventValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Event name is required")
                .Length(3, 200).WithMessage("Event name must be between 3 and 200 characters")
                .Must(BeValidText).WithMessage("Event name contains invalid characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Event description is required")
                .Length(10, 5000).WithMessage("Description must be between 10 and 5000 characters")
                .Must(BeValidHtml).WithMessage("Description contains potentially malicious content");

            RuleFor(x => x.CustomUrl)
                .Length(3, 100).When(x => !string.IsNullOrEmpty(x.CustomUrl))
                .Matches(@"^[a-z0-9\-]+$").When(x => !string.IsNullOrEmpty(x.CustomUrl))
                .WithMessage("Custom URL can only contain lowercase letters, numbers, and hyphens");

            RuleFor(x => x.StartDate)
                .Must(BeInFuture).WithMessage("Event start date must be in the future")
                .LessThan(x => x.EndDate).WithMessage("Start date must be before end date");

            RuleFor(x => x.EndDate)
                .Must(BeReasonableFuture).WithMessage("Event cannot be scheduled more than 2 years in advance");

            RuleFor(x => x.MaxAttendees)
                .InclusiveBetween(1, 100000).WithMessage("Max attendees must be between 1 and 100,000");

            RuleFor(x => x.Venue)
                .NotEmpty().WithMessage("Venue is required")
                .Length(3, 200).WithMessage("Venue must be between 3 and 200 characters")
                .Must(BeValidText).WithMessage("Venue contains invalid characters");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required")
                .Length(5, 500).WithMessage("Address must be between 5 and 500 characters")
                .Must(BeValidAddress).WithMessage("Address contains invalid characters");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required")
                .Length(2, 100).WithMessage("City must be between 2 and 100 characters")
                .Must(BeValidText).WithMessage("City contains invalid characters");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required")
                .Length(2, 100).WithMessage("State must be between 2 and 100 characters")
                .Must(BeValidText).WithMessage("State contains invalid characters");

            RuleFor(x => x.ZipCode)
                .NotEmpty().WithMessage("Zip code is required")
                .Matches(@"^[A-Z0-9\s\-]+$", RegexOptions.IgnoreCase)
                .WithMessage("Invalid zip code format");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required")
                .Length(2, 100).WithMessage("Country must be between 2 and 100 characters")
                .Must(BeValidText).WithMessage("Country contains invalid characters");

            RuleFor(x => x.ImageUrl)
                .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.ImageUrl))
                .WithMessage("Invalid image URL format");

            RuleFor(x => x.Tags)
                .Must(BeValidTags).When(x => !string.IsNullOrEmpty(x.Tags))
                .WithMessage("Tags contain invalid characters");
        }

        private bool BeValidText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            // Prevent common injection patterns
            var dangerousPatterns = new[]
            {
                "<script", "javascript:", "onerror=", "onload=", "onclick=",
                "DROP TABLE", "DELETE FROM", "INSERT INTO", "UPDATE SET",
                "../", "..\\", "%2e%2e", "0x", "\\x"
            };

            var lowerText = text.ToLower();
            foreach (var pattern in dangerousPatterns)
            {
                if (lowerText.Contains(pattern.ToLower()))
                    return false;
            }

            // Allow only safe characters
            return Regex.IsMatch(text, @"^[a-zA-Z0-9\s\-.,!?@#$%&*()_+=\[\]{};:'""\/]+$");
        }

        private bool BeValidHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return false;

            // Strip all HTML tags and check if content is still meaningful
            var stripped = Regex.Replace(html, "<.*?>", string.Empty);
            if (string.IsNullOrWhiteSpace(stripped) || stripped.Length < 10)
                return false;

            // Prevent dangerous HTML patterns
            var dangerousPatterns = new[]
            {
                "<script", "<iframe", "<object", "<embed", "<form",
                "javascript:", "vbscript:", "onload=", "onerror=", "onclick="
            };

            var lowerHtml = html.ToLower();
            foreach (var pattern in dangerousPatterns)
            {
                if (lowerHtml.Contains(pattern))
                    return false;
            }

            return true;
        }

        private bool BeValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;
            
            // Allow alphanumeric, spaces, and common address characters
            return Regex.IsMatch(address, @"^[a-zA-Z0-9\s\-.,#]+$");
        }

        private bool BeValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            
            // Must be a valid URL starting with http or https
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        private bool BeValidTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return false;
            
            // Tags should be comma-separated alphanumeric values
            return Regex.IsMatch(tags, @"^[a-zA-Z0-9\s,\-]+$");
        }

        private bool BeInFuture(DateTime date)
        {
            return date > DateTime.UtcNow;
        }

        private bool BeReasonableFuture(DateTime date)
        {
            return date <= DateTime.UtcNow.AddYears(2);
        }
    }
}