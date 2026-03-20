using System;

namespace PeDeYaf.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public class DocumentNotFoundException(Guid documentId)
    : DomainException($"Document {documentId} was not found.");

public class UserNotFoundException()
    : DomainException("User was not found.");

public class InvalidOtpException(string message)
    : DomainException(message);

public class TooManyRequestsException(string message)
    : DomainException(message);

public class ForbiddenException(string message = "Access denied.")
    : DomainException(message);
