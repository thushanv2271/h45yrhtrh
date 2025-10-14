﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.Organizations;
public static class OrganizationErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Organization.NotFound",
        $"The organization with ID '{id}' was not found");

    public static Error CodeAlreadyExists(string code) => Error.Conflict(
        "Organization.CodeAlreadyExists",
        $"An organization with code '{code}' already exists");

    public static Error EmailAlreadyExists(string email) => Error.Conflict(
        "Organization.EmailAlreadyExists",
        $"An organization with email '{email}' already exists");
}
