﻿using Gestionnaire2.Data;
using Gestionnaire2.DTO;
using Gestionnaire2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Gestionnaire2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        string user;
        int id;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] RegisterDto registerDto)
        {
            if (_context.utilisateurs.Any(u => u.Email == registerDto.Email))
            {
                return BadRequest(new { message = "Cet email est déjà utilisé." });
            }

            // Création de l'utilisateur
            var utilisateur = new Utilisateur
            {
                Email = registerDto.Email,
                MotDePasse = PasswordHasher.HashPassword(registerDto.Password),
                Nom = registerDto.Nom,
                Prenom = registerDto.Prenom,
                DateDeNaissance = registerDto.DateDeNaissance,
                Telephone = registerDto.Telephone,
                IndicatifTelephone = registerDto.IndicatifTelephone,
                Adresse = registerDto.Adresse,
                Ville = registerDto.Ville,
                CodePostal = registerDto.CodePostal,
                Pays = registerDto.Pays,
                GenreId = registerDto.GenreId
            };

            _context.utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();

            // Création du panier pour l'utilisateur
            var panier = new Panier
            {
                UtilisateurId = utilisateur.Id
            };

            _context.paniers.Add(panier);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur et panier créés avec succès." });
        }

        [HttpPost("signin")]
        public IActionResult SignIn([FromBody] LoginDto loginDto)
        {
            var utilisateur = _context.utilisateurs.SingleOrDefault(u => u.Email == loginDto.Email);

            if (utilisateur == null || !PasswordHasher.VerifyPassword(loginDto.Password, utilisateur.MotDePasse))
            {
                return Unauthorized(new { message = "Email ou mot de passe incorrect." });
            }

            // Générer le token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("VotreCleSecretePourJWT1234567890!"); // Assurez-vous que la clé fait au moins 32 caractères
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
               new Claim("id", utilisateur.Id.ToString()),
            new Claim(ClaimTypes.Name, utilisateur.Nom),
            new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString())
        }),
                Expires = DateTime.UtcNow.AddHours(1), // Durée de validité du token
                Issuer = "https://localhost:7249",
                Audience = "https://localhost:7249",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token),
                userName = utilisateur.Nom
            });
        }

        [HttpPut("Update-Profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfilDTO utilisateurDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { message = "Identifiant utilisateur invalide." });
            }

            var utilisateur = await _context.utilisateurs.FirstOrDefaultAsync(u => u.Id == userId);
            if (utilisateur == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            // Mise à jour des champs de l'utilisateur
            utilisateur.Nom = utilisateurDto.Nom;
            utilisateur.Prenom = utilisateurDto.Prenom;
            utilisateur.Email = utilisateurDto.Email;
            utilisateur.DateDeNaissance = utilisateurDto.DateDeNaissance;
            utilisateur.Telephone = utilisateurDto.Telephone;
            utilisateur.IndicatifTelephone = utilisateurDto.IndicatifTelephone;
            utilisateur.Adresse = utilisateurDto.Adresse;
            utilisateur.Ville = utilisateurDto.Ville;
            utilisateur.CodePostal = utilisateurDto.CodePostal;
            utilisateur.Pays = utilisateurDto.Pays;
            utilisateur.GenreId = utilisateurDto.GenreId;
            // Ajoutez ou mettez à jour d'autres champs si nécessaire

            _context.utilisateurs.Update(utilisateur);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profil mis à jour avec succès." });
        }

        [HttpGet("current-user")]
        public IActionResult GetCurrentUser()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            var userName = User.Identity.Name;
            return Ok(new { userName });
        }

        [HttpGet("Get-Profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { message = "Identifiant utilisateur invalide." });
            }

            var utilisateur = await _context.utilisateurs.FirstOrDefaultAsync(u => u.Id == userId);
            if (utilisateur == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            // Retourner l'objet utilisateur
            return Ok(new
            {
                utilisateur.Id,
                utilisateur.Nom,
                utilisateur.Prenom,
                utilisateur.Email,
                utilisateur.GenreId,
                utilisateur.Telephone,
                utilisateur.Adresse,
                utilisateur.Ville,
                utilisateur.CodePostal,
                utilisateur.DateDeNaissance,
                utilisateur.IndicatifTelephone,
                utilisateur.Pays
                
                // Ajouter d'autres champs pertinents
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Supprime toutes les sessions
            return Ok(new { message = "Déconnexion réussie." });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Utilisateur non authentifié." });
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { message = "Identifiant utilisateur invalide." });
            }

            var utilisateur = await _context.utilisateurs.FindAsync(userId);
            if (utilisateur == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            // Vérification de l'ancien mot de passe
            if (!PasswordHasher.VerifyPassword(changePasswordDto.OldPassword, utilisateur.MotDePasse))
            {
                return BadRequest(new { message = "L'ancien mot de passe est incorrect." });
            }
            // Mise à jour avec le nouveau mot de passe
            utilisateur.MotDePasse = PasswordHasher.HashPassword(changePasswordDto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mot de passe mis à jour avec succès." });
        }

        [HttpGet("get-location")]
        public IActionResult GetLocation()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://ipapi.co/json/");
                request.UserAgent = "ipapi.co/#c-sharp-v1.03";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string result = reader.ReadToEnd();
                    return Ok(result); // Return the fetched data as JSON
                }
            }
            catch (WebException ex)
            {
                // Handle the error appropriately
                return StatusCode((int)HttpStatusCode.InternalServerError, "Error fetching location data: " + ex.Message);
            }
        }
    }
}