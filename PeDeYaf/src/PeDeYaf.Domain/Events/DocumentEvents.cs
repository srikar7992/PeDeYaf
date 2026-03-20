using System;

namespace PeDeYaf.Domain.Events;

public record DocumentUploadedEvent(Guid DocumentId, Guid UserId, string S3Key) : IDomainEvent;
public record DocumentProcessedEvent(Guid DocumentId, Guid UserId) : IDomainEvent;
public record OcrCompletedEvent(Guid DocumentId, Guid UserId) : IDomainEvent;
