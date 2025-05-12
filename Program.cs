using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using videoscriptAI.Data;
using videoscriptAI.Models;
using videoscriptAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Diagnostics;
using videoscriptAI.Authorization;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Registra HttpClientFactory PRIMA di altri servizi che potrebbero dipendere da esso
builder.Services.AddHttpClient();

// Inserimento diretto della stringa di connessione
var connectionString = "workstation id=videoscriptAI.mssql.somee.com;packet size=4096;user id=rimpi_SQLLogin_1;pwd=3lymgrctfg;data source=videoscriptAI.mssql.somee.com;persist security info=False;initial catalog=videoscriptAI;TrustServerCertificate=True";
Console.WriteLine($"Utilizzando SQL Server: {connectionString}");

// Configurazione del DbContext con approccio semplificato 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configurazione di Identity con ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configura l'autorizzazione globale - richiede autenticazione per tutte le pagine
const string GLOBAL_AUTH_POLICY = "GlobalAuthPolicy";

// Aggiungi la policy di autorizzazione (corretto duplicazione)
builder.Services.AddAuthorization(options =>
{
    // Policy che richiede l'utente sia admin
    options.AddPolicy("RequireAdminRole", policy =>
        policy.AddRequirements(new AdminRoleRequirement())
    );

    // Policy globale di autenticazione
    options.AddPolicy(GLOBAL_AUTH_POLICY, policy =>
        policy.RequireAuthenticatedUser()
    );
});

builder.Services.AddScoped<IAuthorizationHandler, AdminRoleHandler>();

// Estendi l'autenticazione con Google
builder.Services.AddAuthentication()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.Scope.Add("email");
    googleOptions.Scope.Add("profile");
    googleOptions.CallbackPath = "/api/ExternalAuth/GoogleCallback";
});

// Aggiungi il supporto per la sessione
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registra i servizi dell'applicazione con autorizzazione globale
builder.Services.AddRazorPages(options =>
{

    options.Conventions.AllowAnonymousToPage("/Informazioni");

    // Consenti accesso anonimo a tutte le pagine di autenticazione
    options.Conventions.AllowAnonymousToFolder("/Account");
    options.Conventions.AllowAnonymousToPage("/Error");

    // Richiedi autenticazione per tutto il resto
    options.Conventions.AuthorizeFolder("/", GLOBAL_AUTH_POLICY);

    // Richiedi policy admin per la cartella Admin
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
});


builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
});

// Registra i servizi personalizzati
builder.Services.AddScoped<IAIService, GeminiService>();
builder.Services.AddScoped<IVideoService, VideoService>();

var app = builder.Build();

// Gestione delle migrazioni
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Assicurati che il database esista
        context.Database.EnsureCreated();

        // Verifica lo stato del database ed esegui eventuali migrazioni pendenti
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Applicazione delle migrazioni pendenti...");
            context.Database.Migrate();
            Console.WriteLine("Migrazioni applicate con successo!");
        }
        else
        {
            Console.WriteLine("Database aggiornato, nessuna migrazione pendente.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRORE nell'inizializzazione del database: {ex.Message}");
        if (!app.Environment.IsDevelopment())
        {
            // In ambiente di produzione possiamo decidere se far fallire l'applicazione
            // throw;
        }
    }
}

// Configura la pipeline di richieste HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Aggiungi il middleware della sessione PRIMA dell'autenticazione e autorizzazione
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Importante: mappa sia i controller che le pagine Razor
app.MapControllers();
app.MapRazorPages();

// Aggiungi qui la chiamata all'AdminSeeder
Task.Run(async () =>
{
    try
    {
        Console.WriteLine("Inizializzazione dell'utente amministratore...");
        await AdminSeeder.SeedAdmin(app.Services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore durante l'inizializzazione dell'utente amministratore: {ex.Message}");
    }
}).GetAwaiter().GetResult();

app.Run();