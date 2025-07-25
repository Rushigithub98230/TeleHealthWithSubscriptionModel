using System;
using System.Collections.Generic;

namespace SmartTelehealth.Application.DTOs
{
    public class QuestionnaireTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; }
        public int Version { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class CreateQuestionnaireTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<CreateQuestionDto> Questions { get; set; } = new();
    }

    public class QuestionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int Order { get; set; }
        public string? HelpText { get; set; }
        public string? MediaUrl { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class CreateQuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int Order { get; set; }
        public string? HelpText { get; set; }
        public string? MediaUrl { get; set; }
        public List<CreateQuestionOptionDto> Options { get; set; } = new();
    }

    public class QuestionOptionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Order { get; set; }
        public string? MediaUrl { get; set; }
    }

    public class CreateQuestionOptionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Order { get; set; }
        public string? MediaUrl { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid TemplateId { get; set; }
        public List<UserAnswerDto> Answers { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateUserResponseDto
    {
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid TemplateId { get; set; }
        public List<CreateUserAnswerDto> Answers { get; set; } = new();
    }

    public class UserAnswerDto
    {
        public Guid Id { get; set; }
        public Guid QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public List<Guid> SelectedOptionIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserAnswerDto
    {
        public Guid QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public List<Guid> SelectedOptionIds { get; set; } = new();
    }
} 