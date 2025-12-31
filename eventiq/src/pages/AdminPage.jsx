import React, { useState, useEffect } from 'react';
import { Table, Button, Tag, Space, Modal, Input, Select, message, Typography } from 'antd';
import { CheckOutlined, CloseOutlined, HistoryOutlined, EyeOutlined, SearchOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { adminAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';
import ApprovalHistoryModal from '../components/ApprovalHistoryModal';

const { Title } = Typography;
const { TextArea } = Input;

const AdminPage = () => {
    const navigate = useNavigate();
    const { error, success, contextHolder } = useMessage();
    const [events, setEvents] = useState([]);
    const [loading, setLoading] = useState(false);
    const [pagination, setPagination] = useState({ current: 1, pageSize: 10, total: 0 });
    const [statusFilter, setStatusFilter] = useState('Pending'); // Default là Pending
    const [searchText, setSearchText] = useState('');
    const [approveModalVisible, setApproveModalVisible] = useState(false);
    const [rejectModalVisible, setRejectModalVisible] = useState(false);
    const [selectedEvent, setSelectedEvent] = useState(null);
    const [comment, setComment] = useState('');
    const [historyModalVisible, setHistoryModalVisible] = useState(false);
    const [selectedEventForHistory, setSelectedEventForHistory] = useState(null);

    useEffect(() => {
        fetchEvents();
    }, [pagination.current, pagination.pageSize, statusFilter, searchText]);

    const fetchEvents = async () => {
        setLoading(true);
        try {
            // Nếu statusFilter là 'All' hoặc null, truyền null để lấy tất cả
            const filterValue = statusFilter === 'All' ? null : statusFilter;
            const response = await adminAPI.getEvents(
                pagination.current,
                pagination.pageSize,
                filterValue
            );
            // API returns PaginatedResult: { data: [...], total: ..., page: ..., size: ... }
            let eventsData = response?.data || [];
            
            // Filter theo search text nếu có (client-side filtering)
            if (searchText.trim()) {
                eventsData = eventsData.filter(event => 
                    event.name?.toLowerCase().includes(searchText.toLowerCase())
                );
            }
            
            setEvents(Array.isArray(eventsData) ? eventsData : []);
            // Nếu có search, total sẽ là số lượng filtered, nếu không thì dùng total từ API
            const total = searchText.trim() ? eventsData.length : (response?.total || 0);
            setPagination(prev => ({ ...prev, total }));
        } catch (err) {
            error('Failed to load events');
        } finally {
            setLoading(false);
        }
    };

    const handleApprove = async () => {
        if (!selectedEvent) return;
        try {
            await adminAPI.approveEvent(selectedEvent.id, comment);
            success('Event approved successfully');
            setApproveModalVisible(false);
            setComment('');
            setSelectedEvent(null);
            fetchEvents();
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to approve event');
        }
    };

    const handleReject = async () => {
        if (!selectedEvent || !comment.trim()) {
            message.warning('Please provide a rejection comment');
            return;
        }
        try {
            await adminAPI.rejectEvent(selectedEvent.id, comment);
            success('Event rejected successfully');
            setRejectModalVisible(false);
            setComment('');
            setSelectedEvent(null);
            fetchEvents();
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to reject event');
        }
    };

    const getStatusTag = (status) => {
        const colors = {
            'Draft': 'default',
            'Pending': 'processing',
            'InProgress': 'warning',
            'Published': 'success',
        };
        return <Tag color={colors[status] || 'default'}>{status}</Tag>;
    };

    const columns = [
        {
            title: 'Event Name',
            dataIndex: 'name',
            key: 'name',
        },
        {
            title: 'Organization',
            dataIndex: ['organization', 'name'],
            key: 'organization',
        },
        {
            title: 'Start Date',
            dataIndex: 'start',
            key: 'start',
            render: (date) => new Date(date).toLocaleString(),
        },
        {
            title: 'Status',
            dataIndex: 'status',
            key: 'status',
            render: (status) => getStatusTag(status),
        },
        {
            title: 'Actions',
            key: 'actions',
            render: (_, record) => (
                <Space>
                    {record.status === 'Pending' && (
                        <>
                            <Button
                                type="primary"
                                icon={<CheckOutlined />}
                                onClick={() => {
                                    setSelectedEvent(record);
                                    setApproveModalVisible(true);
                                }}
                            >
                                Approve
                            </Button>
                            <Button
                                danger
                                icon={<CloseOutlined />}
                                onClick={() => {
                                    setSelectedEvent(record);
                                    setRejectModalVisible(true);
                                }}
                            >
                                Reject
                            </Button>
                        </>
                    )}
                    <Button
                        icon={<HistoryOutlined />}
                        onClick={() => {
                            setSelectedEventForHistory(record);
                            setHistoryModalVisible(true);
                        }}
                    >
                        History
                    </Button>
                    <Button
                        icon={<EyeOutlined />}
                        onClick={() => {
                            // Navigate to public event detail page
                            navigate(`/event/${record.id}`);
                        }}
                    >
                        View Detail
                    </Button>
                </Space>
            ),
        },
    ];

    return (
        <div>
            {contextHolder}
            <Title level={2}>Event Management</Title>
            
            <div className="mb-4" style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
                <Input
                    placeholder="Search by event name"
                    prefix={<SearchOutlined />}
                    style={{ width: 300 }}
                    value={searchText}
                    onChange={(e) => {
                        setSearchText(e.target.value);
                        setPagination(prev => ({ ...prev, current: 1 }));
                    }}
                    onPressEnter={fetchEvents}
                    allowClear
                />
                <Select
                    placeholder="Filter by Status"
                    value={statusFilter}
                    style={{ width: 200 }}
                    onChange={(value) => {
                        setStatusFilter(value);
                        setPagination(prev => ({ ...prev, current: 1 }));
                    }}
                >
                    <Select.Option value="All">All</Select.Option>
                    <Select.Option value="Draft">Draft</Select.Option>
                    <Select.Option value="Pending">Pending</Select.Option>
                    <Select.Option value="InProgress">InProgress</Select.Option>
                    <Select.Option value="Published">Published</Select.Option>
                </Select>
            </div>

            <Table
                columns={columns}
                dataSource={events}
                rowKey="id"
                loading={loading}
                pagination={{
                    ...pagination,
                    onChange: (page, pageSize) => {
                        setPagination(prev => ({ ...prev, current: page, pageSize }));
                    },
                }}
            />

            <Modal
                title="Approve Event"
                open={approveModalVisible}
                onOk={handleApprove}
                onCancel={() => {
                    setApproveModalVisible(false);
                    setComment('');
                    setSelectedEvent(null);
                }}
                okText="Approve"
                okButtonProps={{ type: 'primary' }}
            >
                <p>Are you sure you want to approve this event?</p>
                <TextArea
                    placeholder="Optional comment"
                    value={comment}
                    onChange={(e) => setComment(e.target.value)}
                    rows={3}
                />
            </Modal>

            <Modal
                title="Reject Event"
                open={rejectModalVisible}
                onOk={handleReject}
                onCancel={() => {
                    setRejectModalVisible(false);
                    setComment('');
                    setSelectedEvent(null);
                }}
                okText="Reject"
                okButtonProps={{ danger: true }}
            >
                <p>Please provide a reason for rejection:</p>
                <TextArea
                    placeholder="Rejection reason (required)"
                    value={comment}
                    onChange={(e) => setComment(e.target.value)}
                    rows={4}
                    required
                />
            </Modal>

            {selectedEventForHistory && (
                <ApprovalHistoryModal
                    visible={historyModalVisible}
                    eventId={selectedEventForHistory.id}
                    eventName={selectedEventForHistory.name}
                    onClose={() => {
                        setHistoryModalVisible(false);
                        setSelectedEventForHistory(null);
                    }}
                />
            )}
        </div>
    );
};

export default AdminPage;
