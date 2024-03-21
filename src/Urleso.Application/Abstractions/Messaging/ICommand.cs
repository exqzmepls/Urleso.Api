﻿using MediatR;
using Urleso.Domain.Results;

namespace Urleso.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<TypedResult<TResponse>>;