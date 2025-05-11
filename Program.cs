using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using videoscriptAI.Data;
using videoscriptAI.Models;
using videoscriptAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Registra HttpClientFactory PRIMA di altri servizi che potrebbero dipendere da esso
builder.Services.AddHttpClient();

// Inserimento diretto della stringa di connessione
var connectionString = "workstation id=videoscriptAI.mssql.somee.com;packet size=4096;user id=rimpi_SQLLogin_1;pwd=3lymgrctfg;data source=videoscriptAI.mssql.somee.com;persist security info=False;initial catalog=videoscriptAI;TrustServerCertificate=True";
Console.WriteLine($"Utilizzando SQL Server: {connectionString}");

// Configurazione del DbContext con approccio semplificato (simile all'esempio funzionante)
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

// Configura l'autorizzazione globale - richiede autenticazione per tutte le pagine
var globalAuthPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

// Registra i servizi dell'applicazione con autorizzazione globale
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    // Escludiamo solo le pagine di login/account dall'autenticazione obbligatoria
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Account/ResetPassword");
    options.Conventions.AllowAnonymousToPage("/Account/Lockout");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(globalAuthPolicy));
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

app.Run();