namespace TramWebApp.Application.UseCases;

using TramWebApp.Application.Common;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.Exceptions;
using AutoMapper;

public class AuthUseCases : IAuthUseCases
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public AuthUseCases(IAuthService authService, IUserRepository userRepository, IMapper mapper)
    {
        _authService = authService;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var token = await _authService.AuthenticateAsync(dto.Username, dto.Password, cancellationToken);
        if (token == null)
            return null;

        var user = await _userRepository.GetByUsernameAsync(dto.Username, cancellationToken);
        if (user == null)
            return null;

        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return new LoginResponseDto(token, _mapper.Map<UserDto>(user));
    }

    public async Task<UserDto?> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.ExistsByUsernameAsync(dto.Username, cancellationToken))
            throw new ValidationException("Пользователь с таким именем уже существует", new Dictionary<string, string[]> { ["Username"] = ["Пользователь с таким именем уже существует"] });

        if (await _userRepository.ExistsByEmailAsync(dto.Email, cancellationToken))
            throw new ValidationException("Пользователь с таким email уже существует", new Dictionary<string, string[]> { ["Email"] = ["Пользователь с таким email уже существует"] });

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = _mapper.Map<User>(dto, opt => opt.Items["PasswordHash"] = passwordHash);
        
        var created = await _userRepository.AddAsync(user, cancellationToken);
        return _mapper.Map<UserDto>(created);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null ? _mapper.Map<UserProfileDto>(user) : null;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }
}