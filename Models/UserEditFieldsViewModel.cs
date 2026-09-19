using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace NID_Project.Models
{
    public class UserEditFieldsViewModel
    {
        public int ApplicationId { get; set; }
        public List<string> RequestedFields { get; set; } = new();

        public string? FullName { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? Religion { get; set; }
        public string? Occupation { get; set; }
        public string? BloodGroup { get; set; }
        public string? PresentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public IFormFile? Photo { get; set; }
    }
}