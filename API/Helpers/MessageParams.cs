﻿namespace API.Helpers
{
    public class MessageParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        public int UserId { get; set; }
        public string MessageContainer { get; set; } = "Unread";
    }
}