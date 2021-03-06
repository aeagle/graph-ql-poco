using GraphQL;
using GraphQL.Execution;
using GraphQL.Server.Ui.Playground;
using GraphQL.SQLResolver;
using GraphQL.SystemTextJson;
using GraphQLTest.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace GraphQLTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // add execution components
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
            services.AddSingleton<IErrorInfoProvider>(services =>
            {
                var settings = services.GetRequiredService<IOptions<GraphQLSettings>>();
                return new ErrorInfoProvider(
                    new ErrorInfoProviderOptions { 
                        ExposeExceptionStackTrace = settings.Value.ExposeExceptions 
                    }
                );
            });

            // add infrastructure stuff
            services.AddHttpContextAccessor();
            services.AddLogging(builder => builder.AddConsole());

            // add options configuration
            services.Configure<GraphQLSettings>(Configuration);
            services.Configure<GraphQLSettings>(
                settings => settings
                    .BuildUserContext = ctx => new GraphQLUserContext { User = ctx.User }
            );

            services.SetupGraphQLSchema(
                schema => schema
                    .DefaultResolver(
                        new SQLResolver(
                            () => new SqlConnection(
                                @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=GraphQLTest;Integrated Security=SSPI"
                            )
                        )
                    )
                    .Add<Customer>(x => x
                        .EntityConfig(e => e.Table("Customers").Key(f => f.Id))
                        .QueryConfig(q => q.QueryableBy(f => f.Id))
                    )
                    .Add<Order>(x => x
                        .EntityConfig(e => e.Table("Orders").Key(f => f.Id))
                        .QueryConfig(q => q.QueryableBy(f => f.Id))
                    )
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseMiddleware<GraphQLMiddleware>();
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions()
            {
                Path = "/",
                RequestCredentials = RequestCredentials.Include,
                SchemaPollingEnabled = false
            });
            app.UseGraphiQLServer();
            app.UseGraphQLAltair();
            app.UseGraphQLVoyager();
        }
    }
}
