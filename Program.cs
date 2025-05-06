using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using videoscriptAI.Data;
using videoscriptAI.Models;
using videoscriptAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Aggiungi i servizi al container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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

// Estendi l'autenticazione con Google invece di sovrascriverla
builder.Services.AddAuthentication()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.Scope.Add("email");
    googleOptions.Scope.Add("profile");
    googleOptions.CallbackPath = "/signin-google";
});

// Configura i cookie
builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
});

// Registra i servizi dell'applicazione
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// Registra i servizi personalizzati
builder.Services.AddScoped<IAIService, GeminiService>();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();