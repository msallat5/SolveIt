﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSTS.DAL.Models;
using CSTS.DAL.Repository.IRepository;
using FluentValidation;
using FluentValidation.Results;
using CSTS.DAL.Enum;
using CSTS.API.ApiServices;
using CSTS.DAL.AutoMapper.DTOs;
using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity.Data;
using CSTS.DAL;


namespace CSTS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<User> _validator;
        private readonly IMapper _mapper;
        private readonly FileService _fileService;

        public UsersController(IUnitOfWork unitOfWork, IValidator<User> validator, IMapper mapper, FileService fileService)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
            _mapper = mapper;
            _fileService = fileService;
        }

        // GET: api/users
        [HttpGet]
        //[CstsAuth(UserType.ExternalClient)]
        public async Task<ActionResult<APIResponse<IEnumerable<UserResponseDTO>>>> Get([FromQuery] int PageNumber = 1, [FromQuery] int PageSize = 100)
        {
            try
            {
                var response = _unitOfWork.Users.Get(PageNumber, PageSize).Select(u => _mapper.Map<UserResponseDTO>(u));
                return Ok(new APIResponse<IEnumerable<UserResponseDTO>>() { Data = response, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<IEnumerable<UserResponseDTO>> { Data = null, Code = ResponseCode.Error, Message = ex.Message });
            }
        }

        // GET api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<UserResponseDTO>>> Get(Guid id)
        {
            try
            {
                var response = _mapper.Map<UserResponseDTO>(_unitOfWork.Users.GetById(id));

                if (response == null)
                {
                    return Ok(new APIResponse<UserResponseDTO> { Data = null, Code = ResponseCode.Null, Message = "User not found." });
                }
                return Ok(new APIResponse<UserResponseDTO> { Data = response, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<UserResponseDTO> { Data = null, Code = ResponseCode.Error, Message = ex.Message });
            }
        }

        // PUT api/users/5
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<bool>>> Put([FromRoute] Guid id, [FromBody] UserDto inputUser)
        {
            try
            {
                var existingUser = _unitOfWork.Users.GetById(id);
                
                existingUser.FirstName = inputUser.FirstName;
                existingUser.LastName = inputUser.LastName;
                existingUser.MobileNumber = inputUser.MobileNumber;
                existingUser.Email = inputUser.Email;
                existingUser.Image = _fileService.SaveFile( inputUser.UserImage, FolderType.Images);
                existingUser.DateOfBirth = inputUser.DateOfBirth;
                existingUser.Address = inputUser.Address;

                var response = _unitOfWork.Users.Update(existingUser);
                return Ok(new APIResponse<bool> { Data = response, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Error, Message = ex.Message });
            }
        }


        // Activate a user
        [HttpPatch("{id}/activate")]
        [CstsAuth(UserType.SupportManager)]
        public async Task<ActionResult<APIResponse<bool>>> Activate(Guid id)
        {
            try
            {
                var response = _unitOfWork.Users.GetById(id);
                if (response == null)
                {
                    return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Null, Message = "User not found." });
                }

                response.UserStatus = UserStatus.Active;
                var updateResponse = _unitOfWork.Users.Update(response);
                return Ok(new APIResponse<bool> { Data = updateResponse, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Error, Message = ex.Message });
            }
        }

        // Deactivate a user
        [HttpPatch("{id}/deactivate")]
        [CstsAuth(UserType.SupportManager)]
        public async Task<ActionResult<APIResponse<bool>>> Deactivate(Guid id)
        {
            try
            {
                var response = _unitOfWork.Users.GetById(id);
                if (response == null)
                {
                    return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Null, Message = "User not found." });
                }

                response.UserStatus = UserStatus.Deactivated;
                var updateResponse = _unitOfWork.Users.Update(response);
                return Ok(new APIResponse<bool> { Data = updateResponse, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Error, Message = ex.Message });
            }
        }

        // GET: api/users/support-team-members
        [HttpGet("support-team-members")]
        [CstsAuth(UserType.SupportManager)]
        public async Task<ActionResult<APIResponse<IEnumerable<UserResponseDTO>>>> GetSupportTeamMembers()
        {
            try
            {
                var response = _unitOfWork.Users.Find(u => u.UserType == UserType.SupportTeamMember).Select(u => _mapper.Map<UserResponseDTO>(u));
                return Ok(new APIResponse<IEnumerable<UserResponseDTO>> { Data = response, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<IEnumerable<UserResponseDTO>> { Data = null, Code = ResponseCode.Error, Message = ex.Message });
            }
        }

        // GET Clients
        [HttpGet("clients")]
        [CstsAuth(UserType.SupportManager)]
        public async Task<ActionResult<APIResponse<IEnumerable<UserResponseDTO>>>> GetExternalClients([FromQuery] int PageNumber = 1, [FromQuery] int PageSize = 100)
        {
            try
            {
                var response = _unitOfWork.Users.Find(u => u.UserType == UserType.ExternalClient, PageNumber, PageSize).Select(u => _mapper.Map<UserResponseDTO>(u));
                return Ok(new APIResponse<IEnumerable<UserResponseDTO>> { Data = response, Code = ResponseCode.Success, Message = "Success" });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<IEnumerable<UserResponseDTO>>(new List<UserResponseDTO>(), ex.Message));
            }
        }
        [HttpPut("{id}/reset-password")]
        [CstsAuth(UserType.SupportTeamMember, UserType.ExternalClient, UserType.SupportManager)]
        public async Task<ActionResult<APIResponse<bool>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                Guid id = this.GetCurrentUserId();
                var user = _unitOfWork.Users.GetById(id);

                if (user == null)
                {
                    return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Null, Message = "User not found." });
                }

                if (user.UserStatus == UserStatus.Deactivated)
                {
                    return Ok(new APIResponse<bool>(false, "User is Deactivated."));
                }

                if (HashingHelper.CompareHash(resetPasswordDto.OldPassword, user.Password))
                {
                    return Ok(new APIResponse<bool>(false, "Incorrect password."));
                }
                
                if (resetPasswordDto.NewPassword.Count() < 8 )
                {
                    return Ok(new APIResponse<bool>(false, "Password at least 8 characters"));
                }

                user.Password = HashingHelper.GetHashString(resetPasswordDto.NewPassword);
                var response = _unitOfWork.Users.Update(user);

                return Ok(new APIResponse<bool> { Data = response, Code = ResponseCode.Success, Message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return Ok(new APIResponse<bool> { Data = false, Code = ResponseCode.Error, Message = ex.Message });
            }
        }
    }
}