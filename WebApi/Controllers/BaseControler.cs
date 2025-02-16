using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class BaseControler : ControllerBase
{
    private IMediator? _mediator;

    //protected inherit edenler kulansın
    protected IMediator? Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();
}

