using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Admin.Entities;
using IdentityServer4.Admin.Infrastructure;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GrantTypes = IdentityServer4.Models.GrantTypes;

namespace IdentityServer4.Admin
{
    internal class SeedData
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AdminDbContext _dbContext;
        private readonly bool _isDev;

        public SeedData(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dbContext = (AdminDbContext) serviceProvider.GetRequiredService<IDbContext>();
            _isDev = serviceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment();
        }

        public void EnsureData()
        {
            AddBranches();//�����branch���ݣ���Ϊuser����branch
            if (!_dbContext.Users.Any())
            {
                _logger.LogInformation("Seeding database...");

                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = AdminConstants.AdminName,
                    Description = "��������Ա"
                };
                var identityResult = _serviceProvider.GetRequiredService<RoleManager<Role>>().CreateAsync(role).Result;

                if (!identityResult.Succeeded)
                {
                    throw new IdentityServer4AdminException("Create super admin role failed");
                }

                var userMgr = _serviceProvider.GetRequiredService<UserManager<User>>();
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "Administrator",
                    Email = "admin@ids4admin.com",
                    EmailConfirmed = true,
                    CreationTime = DateTime.Now
                };

                var password = _serviceProvider.GetRequiredService<IConfiguration>()["ADMIN_PASSWORD"];
                if (string.IsNullOrWhiteSpace(password))
                {
                    password = "1qazZAQ!";
                }

                identityResult = userMgr.CreateAsync(user, password).Result;
                if (!identityResult.Succeeded)
                {
                    throw new IdentityServer4AdminException("Create super admin user failed");
                }

                identityResult = userMgr.AddToRoleAsync(user, AdminConstants.AdminName).Result;
                _dbContext.Roles.Add(new Role()
                {
                    BranchId = 1000,
                    CreationTime = DateTime.Now,
                    Id = Guid.NewGuid(),
                    Name = "BranchOwner",
                    NormalizedName = "ϵͳ����Ա",

                });
                if (!identityResult.Succeeded)
                {
                    throw new IdentityServer4AdminException("Add super admin user to role failed");
                }

                Commit();

                _logger.LogInformation("Done seeding database");
            }
            else
            {
                if (_isDev)
                {
                    var userMgr = _serviceProvider.GetRequiredService<UserManager<User>>();
                    var admin = userMgr.FindByNameAsync("admin").Result;
                    if (admin != null)
                    {
                        userMgr.DeleteAsync(admin).GetAwaiter().GetResult();
                    }

                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = "admin",
                        Email = "admin@ids4admin.com",
                        EmailConfirmed = true,
                        CreationTime = DateTime.Now
                    };

                    var password = _serviceProvider.GetRequiredService<IConfiguration>()["ADMIN_PASSWORD"];
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        password = "1qazZAQ!";
                    }

                    var identityResult = userMgr.CreateAsync(user, password).Result;
                    if (!identityResult.Succeeded)
                    {
                        throw new IdentityServer4AdminException("Create super admin user failed");
                    }

                    identityResult = userMgr.AddToRoleAsync(user, AdminConstants.AdminName).Result;
                    if (!identityResult.Succeeded)
                    {
                        throw new IdentityServer4AdminException("Add super admin user to role failed");
                    }

                    Commit();
                }

                _logger.LogInformation("Ignore seed database...");
            }

            var userMgr2 = _serviceProvider.GetRequiredService<UserManager<User>>();
            var testUserCount = 15;
            if (_dbContext.Users.Count() < testUserCount)
            {
                for (int i = 0; i < testUserCount; ++i)
                {
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = "testuser" + i,
                        Email = "testuser" + i + "@ids4admin.com",
                        EmailConfirmed = true,
                        CreationTime = DateTime.Now
                    };

                    var password = _serviceProvider.GetRequiredService<IConfiguration>()["ADMIN_PASSWORD"];
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        password = "1qazZAQ!";
                    }

                    userMgr2.CreateAsync(user, password).GetAwaiter().GetResult();
                }
            }

            AddIdentityResources();
            AddApiResources();
            AddClients();
            Commit();
        }

        private void Commit()
        {
            _dbContext.SaveChanges();
        }

        private void AddApiResources()
        {
            if (!_dbContext.ApiResources.Any())
            {
                _logger.LogInformation("ApiResources being populated");
                foreach (var resource in GetApiResources().ToList())
                {
                    _dbContext.ApiResources.Add(resource.ToEntity());
                }
            }
            else
            {
                _logger.LogInformation("ApiResources already populated");
            }
        }

        private void AddIdentityResources()
        {
            if (!_dbContext.IdentityResources.Any())
            {
                _logger.LogInformation("IdentityResources being populated");

                foreach (var resource in GetIdentityResources().ToList())
                {
                    var entity = resource.ToEntity();
                    _dbContext.IdentityResources.Add(entity);
                }
            }
            else
            {
                _logger.LogInformation("IdentityResources already populated");
            }

            Commit();
        }

        private void AddClients()
        {
            if (!_dbContext.Clients.Any())
            {
                _logger.LogInformation("Clients being populated");
                foreach (var client in GetClients().ToList())
                {
                    _dbContext.Clients.Add(client.ToEntity());
                }
            }
            else
            {
                _logger.LogInformation("Clients already populated");
            }
        }

        private void AddBranches()
        {
            if (!_dbContext.Branches.Any())
            {
                _logger.LogInformation("branches�����������");
                foreach (var branch in GetBranches())
                {
                    if (!_isDev)
                    {
                        if (branch.Id == 0)
                            _dbContext.Branches.Add(branch);
                        break;
                    }
                    _dbContext.Branches.Add(branch);
                }
            }
            else
            {
                _logger.LogInformation("branch�����Ѿ������");
            }
        }

        private IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("api1", "My API"),
                new ApiResource("western-research-api", "western-research-api")
            };
        }

        // scopes define the resources in your system
        private IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResource("role", "Your roles", new[] {"role"})
            };
        }

        private IEnumerable<Branch> GetBranches()
        {
            return  new List<Branch>()
            {
                new Branch()
                {
                     Id = 1000,
                     Name = "΢����̫�о�����"
                },
                new Branch()
                {
                    Id = 0,
                    Name = "ϵͳ��ʼ��"
                },
            };
        }

        // clients want to access resources (aka scopes)
        private IEnumerable<Client> GetClients()
        {
            // client credentials client
            return new List<Client>
            {
                new Client
                {
                    ClientId = "client",

                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    // scopes that client has access to
                    AllowedScopes = {"api1"}
                },
                // resource owner password grant client
                new Client
                {
                    ClientId = "ro.client",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = {"api1"}
                },
                // OpenID Connect hybrid flow client (MVC)
                new Client
                {
                    ClientId = "mvc",
                    ClientName = "MVC Client",
                    AllowedGrantTypes = GrantTypes.Hybrid,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    RedirectUris = {"http://localhost:5002/signin-oidc"},
                    PostLogoutRedirectUris = {"http://localhost:5002/signout-callback-oidc"},

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1"
                    },

                    AllowOfflineAccess = true
                },
                // JavaScript Client
                new Client
                {
                    ClientId = "js",
                    ClientName = "JavaScript Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,

                    RedirectUris = {"http://localhost:5003/callback.html"},
                    PostLogoutRedirectUris = {"http://localhost:5003/index.html"},
                    AllowedCorsOrigins = {"http://localhost:5003"},

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1"
                    }
                },
                new Client
                {
                    ClientId = "western-research",
                    ClientName = "western-research",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    AllowedCorsOrigins = {"http://localhost:6568"},
                    RedirectUris = {"http://localhost:6568/signin-oidc"},
                    PostLogoutRedirectUris = {"http://localhost:6568/signout-callback-oidc"},
                    RequireConsent = true,
                    AllowOfflineAccess = false,
                    AccessTokenLifetime = 3600 * 24 * 7,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Phone,
                        "role",
                        "western-research-api"
                    }
                }
            };
        }
    }
}