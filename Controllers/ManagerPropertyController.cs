using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADHUNIK_BARI.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerPropertyController : Controller
    {
        private const long MaximumImageSize = 5 * 1024 * 1024;
        private const long MaximumRequestSize = MaximumImageSize * 2;

        private static readonly string[] ActiveListingStatuses =
        {
            PropertyListingStatuses.Published,
            PropertyListingStatuses.CheckoutReserved,
            PropertyListingStatuses.Rented,
            PropertyListingStatuses.SaleReserved
        };

        private static readonly string[] ManagerActiveStatuses =
        {
            PropertyListingStatuses.Draft,
            PropertyListingStatuses.Published
        };

        private static readonly string[] ManagerReservedOrCompletedStatuses =
        {
            PropertyListingStatuses.CheckoutReserved,
            PropertyListingStatuses.SaleReserved,
            PropertyListingStatuses.Rented,
            PropertyListingStatuses.Closed
        };

        private static readonly IReadOnlyDictionary<string, string[]> AllowedContentTypes =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = new[] { "image/jpeg", "image/jpg" },
                [".jpeg"] = new[] { "image/jpeg", "image/jpg" },
                [".png"] = new[] { "image/png" },
                [".webp"] = new[] { "image/webp" }
            };

        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment environment;
        private readonly ILogger<ManagerPropertyController> logger;

        public ManagerPropertyController(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<ManagerPropertyController> logger)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.environment = environment;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? section)
        {
            var selectedSection = NormalizeManagerSection(section);

            var statusCounts = await dbContext.PropertyListings
                .AsNoTracking()
                .GroupBy(listing => listing.ListingStatus)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(item => item.Status, item => item.Count);

            var query = dbContext.PropertyListings
                .Include(listing => listing.Flat)
                .Include(listing => listing.Applications)
                .AsNoTracking()
                .AsQueryable();

            query = selectedSection switch
            {
                "reserved" => query.Where(listing =>
                    ManagerReservedOrCompletedStatuses.Contains(listing.ListingStatus)),
                "archived" => query.Where(listing =>
                    listing.ListingStatus == PropertyListingStatuses.Archived),
                "all" => query,
                _ => query.Where(listing =>
                    ManagerActiveStatuses.Contains(listing.ListingStatus))
            };

            var listings = await query
                .OrderByDescending(listing => listing.UpdatedAt ?? listing.CreatedAt)
                .ToListAsync();

            int CountFor(string status) => statusCounts.GetValueOrDefault(status);

            var publishedCount = CountFor(PropertyListingStatuses.Published);
            var draftCount = CountFor(PropertyListingStatuses.Draft);
            var reservedOrCompletedCount = ManagerReservedOrCompletedStatuses
                .Sum(CountFor);

            return View(new ManagerPropertyListingsViewModel
            {
                SelectedSection = selectedSection,
                Listings = listings,
                TotalCount = statusCounts.Values.Sum(),
                ActiveCount = publishedCount + draftCount,
                PublishedCount = publishedCount,
                DraftCount = draftCount,
                ReservedOrCompletedCount = reservedOrCompletedCount,
                ArchivedCount = CountFor(PropertyListingStatuses.Archived)
            });
        }


        [HttpGet]
        public async Task<IActionResult> Applications(int? id)
        {
            var query = dbContext.PropertyApplications
                .Include(a => a.PropertyListing)
                    .ThenInclude(l => l!.Flat)
                .Include(a => a.CreatedResidentUser)
                .AsNoTracking()
                .AsQueryable();

            if (id.HasValue)
                query = query.Where(a => a.PropertyListingId == id.Value);

            var applications = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.PropertyListingId = id;
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await PopulateCreateModel(new CreatePropertyListingViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaximumRequestSize)]
        public async Task<IActionResult> Create(CreatePropertyListingViewModel model)
        {
            await ValidateListingInput(
                model.ListingType,
                model.Price,
                model.AdvanceAmount,
                model.RoomLayoutImage,
                model.CoverImage,
                roomLayoutRequired: true);

            var flat = await GetAvailableFlat(model.FlatId);
            if (flat == null)
            {
                ModelState.AddModelError(nameof(model.FlatId), "Select an available and unassigned flat.");
            }

            var createdByUserId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(createdByUserId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(await PopulateCreateModel(model));
            }

            string? coverImagePath = null;
            string? roomLayoutImagePath = null;

            try
            {
                roomLayoutImagePath = await SaveImageAsync(model.RoomLayoutImage!);
                if (model.CoverImage != null)
                {
                    coverImagePath = await SaveImageAsync(model.CoverImage);
                }

                dbContext.PropertyListings.Add(new PropertyListing
                {
                    FlatId = flat!.FlatId,
                    ListingType = model.ListingType,
                    Title = model.Title.Trim(),
                    ShortDescription = model.ShortDescription.Trim(),
                    Description = model.Description.Trim(),
                    Price = model.Price,
                    AdvanceAmount = model.AdvanceAmount,
                    Bedrooms = model.Bedrooms,
                    Bathrooms = model.Bathrooms,
                    Balconies = model.Balconies,
                    AreaSqFt = model.AreaSqFt,
                    FurnishingStatus = NormalizeOptional(model.FurnishingStatus),
                    Facing = NormalizeOptional(model.Facing),
                    Features = NormalizeOptional(model.Features),
                    CoverImagePath = coverImagePath,
                    RoomLayoutImagePath = roomLayoutImagePath,
                    ListingStatus = PropertyListingStatuses.Draft,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = createdByUserId
                });

                await dbContext.SaveChangesAsync();
                TempData["Success"] = $"Listing for Flat {flat.FlatNumber} was created as a draft.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                DeleteStoredImage(roomLayoutImagePath);
                DeleteStoredImage(coverImagePath);
                logger.LogError(ex, "Error creating a property listing for Flat {FlatId}.", model.FlatId);
                ModelState.AddModelError(string.Empty, "The listing could not be created. Please try again.");
                return View(await PopulateCreateModel(model));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var listing = await dbContext.PropertyListings
                .Include(item => item.Flat)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.PropertyListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            return View(ToEditModel(listing));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaximumRequestSize)]
        public async Task<IActionResult> Edit(EditPropertyListingViewModel model)
        {
            var listing = await dbContext.PropertyListings
                .Include(item => item.Flat)
                .SingleOrDefaultAsync(item => item.PropertyListingId == model.PropertyListingId);

            if (listing == null)
            {
                return NotFound();
            }

            await ValidateListingInput(
                model.ListingType,
                model.Price,
                model.AdvanceAmount,
                model.RoomLayoutImage,
                model.CoverImage,
                roomLayoutRequired: false);

            if (model.RoomLayoutImage == null && string.IsNullOrWhiteSpace(listing.RoomLayoutImagePath))
            {
                ModelState.AddModelError(nameof(model.RoomLayoutImage), "A room-layout image is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(PopulateEditModel(model, listing));
            }

            string? newCoverImagePath = null;
            string? newRoomLayoutImagePath = null;
            var previousCoverImagePath = listing.CoverImagePath;
            var previousRoomLayoutImagePath = listing.RoomLayoutImagePath;

            try
            {
                if (model.CoverImage != null)
                {
                    newCoverImagePath = await SaveImageAsync(model.CoverImage);
                }

                if (model.RoomLayoutImage != null)
                {
                    newRoomLayoutImagePath = await SaveImageAsync(model.RoomLayoutImage);
                }

                listing.ListingType = model.ListingType;
                listing.Title = model.Title.Trim();
                listing.ShortDescription = model.ShortDescription.Trim();
                listing.Description = model.Description.Trim();
                listing.Price = model.Price;
                listing.AdvanceAmount = model.AdvanceAmount;
                listing.Bedrooms = model.Bedrooms;
                listing.Bathrooms = model.Bathrooms;
                listing.Balconies = model.Balconies;
                listing.AreaSqFt = model.AreaSqFt;
                listing.FurnishingStatus = NormalizeOptional(model.FurnishingStatus);
                listing.Facing = NormalizeOptional(model.Facing);
                listing.Features = NormalizeOptional(model.Features);
                listing.CoverImagePath = newCoverImagePath ?? listing.CoverImagePath;
                listing.RoomLayoutImagePath = newRoomLayoutImagePath ?? listing.RoomLayoutImagePath;
                listing.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                if (newCoverImagePath != null)
                {
                    DeleteStoredImage(previousCoverImagePath);
                }

                if (newRoomLayoutImagePath != null)
                {
                    DeleteStoredImage(previousRoomLayoutImagePath);
                }

                TempData["Success"] = $"Listing for Flat {listing.Flat?.FlatNumber ?? listing.FlatId.ToString()} was updated.";
                return RedirectToAction(nameof(Details), new { id = listing.PropertyListingId });
            }
            catch (Exception ex)
            {
                DeleteStoredImage(newRoomLayoutImagePath);
                DeleteStoredImage(newCoverImagePath);
                logger.LogError(ex, "Error updating property listing {PropertyListingId}.", model.PropertyListingId);
                ModelState.AddModelError(string.Empty, "The listing could not be updated. Please try again.");
                return View(PopulateEditModel(model, listing));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var listing = await dbContext.PropertyListings
                .Include(item => item.Flat)
                .Include(item => item.CreatedByUser)
                .Include(item => item.Applications)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.PropertyListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var listing = await dbContext.PropertyListings
                .SingleOrDefaultAsync(item => item.PropertyListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            if (listing.ListingStatus == PropertyListingStatuses.Published)
            {
                TempData["Success"] = "This listing is already published.";
                return RedirectToAction(nameof(Index));
            }

            if (listing.ListingStatus != PropertyListingStatuses.Draft)
            {
                TempData["Error"] = "Only draft listings can be published.";
                return RedirectToAction(nameof(Index));
            }

            await ValidatePublishableListing(listing);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message)));
                return RedirectToAction(nameof(Details), new { id });
            }

            listing.ListingStatus = PropertyListingStatuses.Published;
            listing.PublishedAt = DateTime.UtcNow;
            listing.UpdatedAt = DateTime.UtcNow;

            try
            {
                await dbContext.SaveChangesAsync();
                TempData["Success"] = "Listing published successfully.";
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Listing publish conflict for property listing {PropertyListingId}.", id);
                TempData["Error"] = "The listing could not be published because the flat already has another active listing.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id)
        {
            var listing = await dbContext.PropertyListings
                .SingleOrDefaultAsync(item => item.PropertyListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            if (listing.ListingStatus == PropertyListingStatuses.Draft)
            {
                TempData["Success"] = "This listing is already unpublished.";
                return RedirectToAction(nameof(Index));
            }

            if (listing.ListingStatus != PropertyListingStatuses.Published)
            {
                TempData["Error"] = "Only published listings can be unpublished.";
                return RedirectToAction(nameof(Index));
            }

            listing.ListingStatus = PropertyListingStatuses.Draft;
            listing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            TempData["Success"] = "Listing unpublished and returned to draft status.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var listing = await dbContext.PropertyListings
                .SingleOrDefaultAsync(item => item.PropertyListingId == id);

            if (listing == null)
            {
                return NotFound();
            }

            if (listing.ListingStatus != PropertyListingStatuses.Archived)
            {
                listing.ListingStatus = PropertyListingStatuses.Archived;
                listing.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                TempData["Success"] = "Listing archived. Existing listing and application history was preserved.";
            }
            else
            {
                TempData["Success"] = "This listing is already archived.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateListingInput(
            string listingType,
            decimal price,
            decimal advanceAmount,
            IFormFile? roomLayoutImage,
            IFormFile? coverImage,
            bool roomLayoutRequired)
        {
            if (listingType != PropertyListingTypes.ToLet && listingType != PropertyListingTypes.ForSale)
            {
                ModelState.AddModelError(nameof(CreatePropertyListingViewModel.ListingType), "Select a valid listing type.");
            }

            if (price <= 0)
            {
                ModelState.AddModelError(nameof(CreatePropertyListingViewModel.Price), "Price must be greater than zero.");
            }

            if (advanceAmount <= 0)
            {
                ModelState.AddModelError(nameof(CreatePropertyListingViewModel.AdvanceAmount), "Advance amount must be greater than zero.");
            }

            await ValidateImageAsync(roomLayoutImage, nameof(CreatePropertyListingViewModel.RoomLayoutImage), roomLayoutRequired);
            await ValidateImageAsync(coverImage, nameof(CreatePropertyListingViewModel.CoverImage), required: false);
        }

        private async Task ValidatePublishableListing(PropertyListing listing)
        {
            if (listing.ListingType != PropertyListingTypes.ToLet && listing.ListingType != PropertyListingTypes.ForSale)
            {
                ModelState.AddModelError(nameof(listing.ListingType), "The listing type is invalid.");
            }

            if (listing.Price <= 0)
            {
                ModelState.AddModelError(nameof(listing.Price), "Price must be greater than zero.");
            }

            if (listing.AdvanceAmount <= 0)
            {
                ModelState.AddModelError(nameof(listing.AdvanceAmount), "Advance amount must be greater than zero.");
            }

            if (!IsSafeStoredImagePath(listing.RoomLayoutImagePath) || !System.IO.File.Exists(GetStoredImageFullPath(listing.RoomLayoutImagePath)))
            {
                ModelState.AddModelError(nameof(listing.RoomLayoutImagePath), "The room-layout image is missing or invalid.");
            }

            var flat = await dbContext.Flats
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.FlatId == listing.FlatId);

            if (flat == null || flat.FlatStatus != "Available")
            {
                ModelState.AddModelError(nameof(listing.FlatId), "The flat is not currently available.");
            }
            else if (await dbContext.FlatAssignments.AnyAsync(assignment =>
                         assignment.FlatId == listing.FlatId && assignment.IsActive))
            {
                ModelState.AddModelError(nameof(listing.FlatId), "The flat already has an active assignment.");
            }

            if (await dbContext.PropertyListings.AnyAsync(other =>
                    other.PropertyListingId != listing.PropertyListingId &&
                    other.FlatId == listing.FlatId &&
                    ActiveListingStatuses.Contains(other.ListingStatus)))
            {
                ModelState.AddModelError(string.Empty, "This flat already has another active listing.");
            }
        }

        private async Task<Flat?> GetAvailableFlat(int flatId)
        {
            return await dbContext.Flats
                .Where(flat => flat.FlatId == flatId &&
                    flat.FlatStatus == "Available" &&
                    !dbContext.FlatAssignments.Any(assignment =>
                        assignment.FlatId == flat.FlatId && assignment.IsActive))
                .SingleOrDefaultAsync();
        }

        private async Task<CreatePropertyListingViewModel> PopulateCreateModel(CreatePropertyListingViewModel model)
        {
            model.AvailableFlats = await dbContext.Flats
                .AsNoTracking()
                .Where(flat => flat.FlatStatus == "Available" &&
                    !dbContext.FlatAssignments.Any(assignment =>
                        assignment.FlatId == flat.FlatId && assignment.IsActive))
                .OrderBy(flat => flat.FloorNumber)
                .ThenBy(flat => flat.FlatNumber)
                .ToListAsync();

            return model;
        }

        private static EditPropertyListingViewModel ToEditModel(PropertyListing listing)
        {
            return new EditPropertyListingViewModel
            {
                PropertyListingId = listing.PropertyListingId,
                FlatId = listing.FlatId,
                FlatNumber = listing.Flat?.FlatNumber ?? string.Empty,
                FloorNumber = listing.Flat?.FloorNumber ?? 0,
                ListingStatus = listing.ListingStatus,
                ListingType = listing.ListingType,
                Title = listing.Title,
                ShortDescription = listing.ShortDescription,
                Description = listing.Description,
                Price = listing.Price,
                AdvanceAmount = listing.AdvanceAmount,
                Bedrooms = listing.Bedrooms,
                Bathrooms = listing.Bathrooms,
                Balconies = listing.Balconies,
                AreaSqFt = listing.AreaSqFt,
                FurnishingStatus = listing.FurnishingStatus,
                Facing = listing.Facing,
                Features = listing.Features,
                CurrentCoverImagePath = listing.CoverImagePath,
                CurrentRoomLayoutImagePath = listing.RoomLayoutImagePath
            };
        }

        private static EditPropertyListingViewModel PopulateEditModel(
            EditPropertyListingViewModel model,
            PropertyListing listing)
        {
            model.FlatId = listing.FlatId;
            model.FlatNumber = listing.Flat?.FlatNumber ?? string.Empty;
            model.FloorNumber = listing.Flat?.FloorNumber ?? 0;
            model.ListingStatus = listing.ListingStatus;
            model.CurrentCoverImagePath = listing.CoverImagePath;
            model.CurrentRoomLayoutImagePath = listing.RoomLayoutImagePath;
            return model;
        }

        private async Task ValidateImageAsync(IFormFile? file, string modelKey, bool required)
        {
            if (file == null)
            {
                if (required)
                {
                    ModelState.AddModelError(modelKey, "An image is required.");
                }

                return;
            }

            if (file.Length <= 0)
            {
                ModelState.AddModelError(modelKey, "The image cannot be empty.");
                return;
            }

            if (file.Length > MaximumImageSize)
            {
                ModelState.AddModelError(modelKey, "Each image must be 5 MB or smaller.");
                return;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedContentTypes.TryGetValue(extension, out var contentTypes))
            {
                ModelState.AddModelError(modelKey, "Only JPG, JPEG, PNG, and WebP images are allowed.");
                return;
            }

            if (!contentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(modelKey, "The uploaded file content type does not match its extension.");
                return;
            }

            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (!MatchesImageSignature(extension, header, bytesRead))
            {
                ModelState.AddModelError(modelKey, "The uploaded file is not a valid supported image.");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "properties");
            Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, fileName);
            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true);
            await file.CopyToAsync(stream);

            return $"/uploads/properties/{fileName}";
        }

        private void DeleteStoredImage(string? imagePath)
        {
            if (!IsSafeStoredImagePath(imagePath))
            {
                return;
            }

            try
            {
                var fullPath = GetStoredImageFullPath(imagePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete stored property image {ImagePath}.", imagePath);
            }
        }

        private string GetStoredImageFullPath(string imagePath)
        {
            var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(environment.WebRootPath, relativePath));
        }

        private static bool IsSafeStoredImagePath(string? imagePath)
        {
            return !string.IsNullOrWhiteSpace(imagePath) &&
                   imagePath.StartsWith("/uploads/properties/", StringComparison.OrdinalIgnoreCase) &&
                   !imagePath.Contains("..", StringComparison.Ordinal);
        }

        private static bool MatchesImageSignature(string extension, byte[] header, int bytesRead)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
                ".webp" => bytesRead >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => false
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeManagerSection(string? section)
        {
            return section?.Trim().ToLowerInvariant() switch
            {
                "reserved" => "reserved",
                "archived" => "archived",
                "all" => "all",
                _ => "active"
            };
        }
    }
}
