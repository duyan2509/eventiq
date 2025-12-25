import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  Table, 
  Button, 
  Modal, 
  Form, 
  Input, 
  DatePicker, 
  Select, 
  Tag, 
  Space, 
  Card, 
  Tabs,
  message,
  Popconfirm,
  Divider
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UserAddOutlined } from '@ant-design/icons';
import { staffAPI } from '../services/api';
import { signalRService } from '../services/signalr';
import { useMessage } from '../hooks/useMessage';
import dayjs from 'dayjs';

const { RangePicker } = DatePicker;
const { TextArea } = Input;

const StaffManagement = () => {
  const params = useParams();
  const orgId = params.orgId?.trim();
  const eventId = params.eventId?.trim();
  const navigate = useNavigate();
  const { success, error } = useMessage();
  const [loading, setLoading] = useState(false);
  const [staffs, setStaffs] = useState([]);
  const [tasks, setTasks] = useState([]);
  const [invitations, setInvitations] = useState([]);
  const [activeTab, setActiveTab] = useState('staffs');
  const [selectedTask, setSelectedTask] = useState(null);
  const [taskOptions, setTaskOptions] = useState([]);

  // Modals
  const [inviteModalVisible, setInviteModalVisible] = useState(false);
  const [taskModalVisible, setTaskModalVisible] = useState(false);
  const [optionModalVisible, setOptionModalVisible] = useState(false);
  const [assignTaskModalVisible, setAssignTaskModalVisible] = useState(false);
  const [editingTask, setEditingTask] = useState(null);
  const [editingOption, setEditingOption] = useState(null);

  const [inviteForm] = Form.useForm();
  const [taskForm] = Form.useForm();
  const [optionForm] = Form.useForm();
  const [assignForm] = Form.useForm();

  useEffect(() => {
    fetchData();
    
    // Setup SignalR
    const token = localStorage.getItem('token');
    if (token) {
      signalRService.connect(token).then(() => {
        signalRService.joinEventGroup(eventId);
      });
    }

    // Register SignalR event handlers
    const handleTaskAssigned = () => {
      fetchData();
      message.success('Task assignment updated');
    };

    const handleTaskCreated = () => {
      fetchData();
      message.success('New task created');
    };

    const handleTaskUpdated = () => {
      fetchData();
      message.success('Task updated');
    };

    const handleTaskDeleted = () => {
      fetchData();
      message.success('Task deleted');
    };

    const handleTaskOptionCreated = () => {
      if (selectedTask) {
        loadTaskOptions(selectedTask.id);
      }
      fetchData();
      message.success('Task option created');
    };

    const handleTaskOptionUpdated = () => {
      if (selectedTask) {
        loadTaskOptions(selectedTask.id);
      }
      fetchData();
      message.success('Task option updated');
    };

    const handleTaskOptionDeleted = () => {
      if (selectedTask) {
        loadTaskOptions(selectedTask.id);
      }
      fetchData();
      message.success('Task option deleted');
    };

    signalRService.on('TaskAssigned', handleTaskAssigned);
    signalRService.on('TaskUnassigned', handleTaskAssigned);
    signalRService.on('TaskCreated', handleTaskCreated);
    signalRService.on('TaskUpdated', handleTaskUpdated);
    signalRService.on('TaskDeleted', handleTaskDeleted);
    signalRService.on('TaskOptionCreated', handleTaskOptionCreated);
    signalRService.on('TaskOptionUpdated', handleTaskOptionUpdated);
    signalRService.on('TaskOptionDeleted', handleTaskOptionDeleted);

    return () => {
      signalRService.off('TaskAssigned', handleTaskAssigned);
      signalRService.off('TaskUnassigned', handleTaskAssigned);
      signalRService.off('TaskCreated', handleTaskCreated);
      signalRService.off('TaskUpdated', handleTaskUpdated);
      signalRService.off('TaskDeleted', handleTaskDeleted);
      signalRService.off('TaskOptionCreated', handleTaskOptionCreated);
      signalRService.off('TaskOptionUpdated', handleTaskOptionUpdated);
      signalRService.off('TaskOptionDeleted', handleTaskOptionDeleted);
      signalRService.leaveEventGroup(eventId);
    };
  }, [eventId, selectedTask]);

  const fetchData = async () => {
    setLoading(true);
    try {
      if (!eventId) {
        error('Event ID is required');
        return;
      }
      const [staffsData, tasksData, invitationsData] = await Promise.all([
        staffAPI.getEventStaffs(eventId),
        staffAPI.getEventTasks(eventId),
        staffAPI.getEventInvitations(eventId)
      ]);
      // Backend returns StaffListDto with property "Staffs" (capital S)
      setStaffs(staffsData?.Staffs || staffsData?.staffs || []);
      setTasks(tasksData || []);
      setInvitations(invitationsData || []);
    } catch (err) {
      console.error('Error fetching data:', err);
      error(err.response?.data?.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const loadTaskOptions = async (taskId) => {
    try {
      const options = await staffAPI.getTaskOptions(eventId, taskId);
      setTaskOptions(options || []);
    } catch (err) {
      error('Failed to load task options');
    }
  };

  const handleInviteStaff = async (values) => {
    try {
      await staffAPI.inviteStaff({
        eventId,
        organizationId: orgId,
        invitedUserEmail: values.email,
        eventStartTime: values.timeRange[0].toISOString(),
        eventEndTime: values.timeRange[1].toISOString(),
        inviteExpiredAt: values.expiredAt.toISOString()
      });
      success('Invitation sent successfully');
      setInviteModalVisible(false);
      inviteForm.resetFields();
      fetchData();
    } catch (err) {
      const errorMessage = err.response?.data?.message || 'Failed to send invitation';
      error(errorMessage);
    }
  };

  const handleCreateTask = async (values) => {
    try {
      // Get options from form or state
      const options = taskOptions.filter(opt => opt.trim() !== '');
      if (options.length === 0) {
        error('Please add at least one option for the task');
        return;
      }

      await staffAPI.createTask({
        eventId,
        name: values.name,
        description: values.description,
        options: options
      });
      success('Task created successfully');
      setTaskModalVisible(false);
      setTaskOptions([]);
      taskForm.resetFields();
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to create task');
    }
  };

  const handleUpdateTask = async (values) => {
    try {
      await staffAPI.updateTask(eventId, editingTask.id, {
        name: values.name,
        description: values.description
      });
      success('Task updated successfully');
      setTaskModalVisible(false);
      setEditingTask(null);
      taskForm.resetFields();
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to update task');
    }
  };

  const handleDeleteTask = async (taskId) => {
    try {
      await staffAPI.deleteTask(eventId, taskId);
      success('Task deleted successfully');
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to delete task');
    }
  };

  const handleCreateOption = async (values) => {
    try {
      await staffAPI.createTaskOption(eventId, selectedTask.id, {
        taskId: selectedTask.id,
        optionName: values.optionName
      });
      success('Task option created successfully');
      setOptionModalVisible(false);
      optionForm.resetFields();
      loadTaskOptions(selectedTask.id);
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to create task option');
    }
  };

  const handleUpdateOption = async (values) => {
    try {
      await staffAPI.updateTaskOption(eventId, selectedTask.id, editingOption.id, {
        optionName: values.optionName
      });
      success('Task option updated successfully');
      setOptionModalVisible(false);
      setEditingOption(null);
      optionForm.resetFields();
      loadTaskOptions(selectedTask.id);
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to update task option');
    }
  };

  const handleDeleteOption = async (optionId) => {
    try {
      await staffAPI.deleteTaskOption(eventId, selectedTask.id, optionId);
      success('Task option deleted successfully');
      loadTaskOptions(selectedTask.id);
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to delete task option');
    }
  };

  const handleAssignTask = async (values) => {
    try {
      await staffAPI.assignTaskToStaff(eventId, {
        taskId: values.taskId,
        optionId: values.optionId,
        staffId: values.staffId
      });
      success('Task assigned successfully');
      setAssignTaskModalVisible(false);
      assignForm.resetFields();
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to assign task');
    }
  };

  const handleUnassignTask = async (taskId, optionId, staffId) => {
    try {
      await staffAPI.unassignTaskFromStaff(eventId, taskId, optionId, staffId);
      success('Task unassigned successfully');
      fetchData();
    } catch (err) {
      error(err.response?.data?.message || 'Failed to unassign task');
    }
  };

  const openEditTaskModal = (task) => {
    setEditingTask(task);
    taskForm.setFieldsValue({
      name: task.name,
      description: task.description
    });
    setTaskModalVisible(true);
  };

  const openEditOptionModal = (option) => {
    setEditingOption(option);
    optionForm.setFieldsValue({
      optionName: option.optionName
    });
    setOptionModalVisible(true);
  };

  const handleTaskSelect = (taskId) => {
    const task = tasks.find(t => t.id === taskId);
    setSelectedTask(task);
    if (task) {
      loadTaskOptions(task.id);
    }
  };

  const staffColumns = [
    {
      title: 'User',
      key: 'user',
      render: (_, record) => (
        <div>
          <div>{record.userName || 'N/A'}</div>
          <div className="text-gray-500 text-sm">{record.userEmail}</div>
        </div>
      )
    },
    {
      title: 'Role',
      dataIndex: 'role',
      render: () => <Tag color="blue">Staff</Tag>
    },
    {
      title: 'Work Time',
      key: 'workTime',
      render: (_, record) => (
        <div>
          {dayjs(record.startTime).format('DD/MM/YYYY HH:mm')} - {dayjs(record.endTime).format('DD/MM/YYYY HH:mm')}
        </div>
      )
    },
    {
      title: 'Assigned Tasks',
      key: 'tasks',
      render: (_, record) => (
        <div>
          {record.assignedTasks?.length > 0 ? (
            record.assignedTasks.map(task => (
              <Tag key={`${task.taskId}-${task.optionId}`} className="mb-1">
                {task.taskName} - {task.optionName}
              </Tag>
            ))
          ) : (
            <span className="text-gray-400">No tasks assigned</span>
          )}
        </div>
      )
    }
  ];

  const taskColumns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name'
    },
    {
      title: 'Options',
      key: 'options',
      render: (_, record) => (
        <div>
          {record.options?.length > 0 ? (
            record.options.map(option => (
              <Tag key={option.id} className="mb-1">
                {option.optionName}
              </Tag>
            ))
          ) : (
            <span className="text-gray-400">No options</span>
          )}
        </div>
      )
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button 
            icon={<EditOutlined />} 
            size="small"
            onClick={() => openEditTaskModal(record)}
            disabled={record.isDefault}
          >
            Edit
          </Button>
          <Popconfirm
            title="Delete task?"
            description="Are you sure you want to delete this task?"
            onConfirm={() => handleDeleteTask(record.id)}
            okText="Yes"
            cancelText="No"
          >
            <Button 
              icon={<DeleteOutlined />} 
              size="small" 
              danger
              disabled={record.isDefault}
            >
              Delete
            </Button>
          </Popconfirm>
          <Button 
            size="small"
            onClick={() => {
              setSelectedTask(record);
              loadTaskOptions(record.id);
              setOptionModalVisible(true);
            }}
          >
            Manage Options
          </Button>
          <Button 
            size="small"
            onClick={() => {
              setSelectedTask(record);
              loadTaskOptions(record.id);
              assignForm.setFieldsValue({ taskId: record.id, optionId: undefined });
              setAssignTaskModalVisible(true);
            }}
          >
            Assign Staff
          </Button>
        </Space>
      )
    }
  ];

  const invitationColumns = [
    {
      title: 'Invited User',
      key: 'user',
      render: (_, record) => (
        <div>
          <div>{record.invitedUserName || 'N/A'}</div>
          <div className="text-gray-500 text-sm">{record.invitedUserEmail}</div>
        </div>
      )
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status) => {
        const colorMap = {
          Pending: 'orange',
          Accepted: 'green',
          Rejected: 'red',
          Expired: 'gray'
        };
        return <Tag color={colorMap[status]}>{status}</Tag>;
      }
    },
    {
      title: 'Event Time',
      key: 'eventTime',
      render: (_, record) => (
        <div>
          {dayjs(record.eventStartTime).format('DD/MM/YYYY HH:mm')} - {dayjs(record.eventEndTime).format('DD/MM/YYYY HH:mm')}
        </div>
      )
    },
    {
      title: 'Expires At',
      dataIndex: 'inviteExpiredAt',
      key: 'inviteExpiredAt',
      render: (text) => dayjs(text).format('DD/MM/YYYY HH:mm')
    }
  ];

  return (
    <div className="p-6">
      <div className="mb-4 flex justify-between items-center">
        <h1 className="text-2xl font-bold">Staff Management</h1>
        <Button onClick={() => navigate(`/org/${orgId}`)}>Back to Events</Button>
      </div>

      <Tabs activeKey={activeTab} onChange={setActiveTab}>
        <Tabs.TabPane tab="Staffs" key="staffs">
          <Card
            title="Event Staffs"
            extra={
              <Button 
                type="primary" 
                icon={<UserAddOutlined />}
                onClick={() => setInviteModalVisible(true)}
              >
                Invite Staff
              </Button>
            }
          >
            <Table
              columns={staffColumns}
              dataSource={staffs}
              rowKey="id"
              loading={loading}
            />
          </Card>
        </Tabs.TabPane>

        <Tabs.TabPane tab="Tasks" key="tasks">
          <Card
            title="Event Tasks"
            extra={
              <Button 
                type="primary" 
                icon={<PlusOutlined />}
                onClick={() => {
                  setEditingTask(null);
                  setTaskOptions([]);
                  taskForm.resetFields();
                  setTaskModalVisible(true);
                }}
              >
                Add Task
              </Button>
            }
          >
            <Table
              columns={taskColumns}
              dataSource={tasks}
              rowKey="id"
              loading={loading}
            />
          </Card>
        </Tabs.TabPane>

        <Tabs.TabPane tab="Invitations" key="invitations">
          <Card title="Staff Invitations">
            <Table
              columns={invitationColumns}
              dataSource={invitations}
              rowKey="id"
              loading={loading}
            />
          </Card>
        </Tabs.TabPane>
      </Tabs>

      {/* Invite Staff Modal */}
      <Modal
        title="Invite Staff"
        open={inviteModalVisible}
        onCancel={() => {
          setInviteModalVisible(false);
          inviteForm.resetFields();
        }}
        onOk={() => inviteForm.submit()}
      >
        <Form form={inviteForm} onFinish={handleInviteStaff} layout="vertical">
          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Please enter email' },
              { type: 'email', message: 'Please enter a valid email' }
            ]}
          >
            <Input placeholder="Enter user email" />
          </Form.Item>
          <Form.Item
            name="timeRange"
            label="Work Time"
            rules={[{ required: true, message: 'Please select work time' }]}
          >
            <RangePicker showTime format="DD/MM/YYYY HH:mm" />
          </Form.Item>
          <Form.Item
            name="expiredAt"
            label="Invitation Expires At"
            rules={[{ required: true, message: 'Please select expiration date' }]}
          >
            <DatePicker showTime format="DD/MM/YYYY HH:mm" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Task Modal */}
      <Modal
        title={editingTask ? 'Edit Task' : 'Create Task'}
        open={taskModalVisible}
        onCancel={() => {
          setTaskModalVisible(false);
          setEditingTask(null);
          taskForm.resetFields();
        }}
        onOk={() => taskForm.submit()}
      >
        <Form 
          form={taskForm} 
          onFinish={editingTask ? handleUpdateTask : handleCreateTask} 
          layout="vertical"
        >
          <Form.Item
            name="name"
            label="Task Name"
            rules={[{ required: true, message: 'Please enter task name' }]}
          >
            <Input placeholder="e.g., Scan ticket, Lau nhà" />
          </Form.Item>
          <Form.Item
            name="description"
            label="Description"
          >
            <TextArea rows={3} placeholder="Task description" />
          </Form.Item>
          {!editingTask && (
            <Form.Item
              label="Task Options"
              required
              help="Add options for this task (e.g., Gate A, Gate B for Scan ticket; Tầng 1, Tầng 2 for Lau nhà)"
            >
              <Select
                mode="tags"
                placeholder="Enter option names and press Enter (e.g., Gate A, Gate B)"
                value={taskOptions}
                onChange={setTaskOptions}
                style={{ width: '100%' }}
                tokenSeparators={[',']}
              />
            </Form.Item>
          )}
        </Form>
      </Modal>

      {/* Task Option Modal */}
      <Modal
        title={editingOption ? 'Edit Task Option' : 'Create Task Option'}
        open={optionModalVisible}
        onCancel={() => {
          setOptionModalVisible(false);
          setEditingOption(null);
          setSelectedTask(null);
          optionForm.resetFields();
        }}
        onOk={() => optionForm.submit()}
        width={600}
      >
        {selectedTask && (
          <div className="mb-4">
            <p><strong>Task:</strong> {selectedTask.name}</p>
          </div>
        )}
        <Form 
          form={optionForm} 
          onFinish={editingOption ? handleUpdateOption : handleCreateOption} 
          layout="vertical"
        >
          <Form.Item
            name="optionName"
            label="Option Name"
            rules={[{ required: true, message: 'Please enter option name' }]}
          >
            <Input placeholder="e.g., Gate A, Gate B, Main Gate" />
          </Form.Item>
        </Form>
        <Divider />
        <div>
          <h4>Existing Options:</h4>
          {taskOptions.length > 0 ? (
            <Space wrap>
              {taskOptions.map(option => (
                <div key={option.id} className="flex items-center gap-2 mb-2">
                  <Tag>{option.optionName}</Tag>
                  <Button 
                    size="small" 
                    icon={<EditOutlined />}
                    onClick={() => openEditOptionModal(option)}
                  />
                  <Popconfirm
                    title="Delete option?"
                    description="Are you sure you want to delete this option?"
                    onConfirm={() => handleDeleteOption(option.id)}
                    okText="Yes"
                    cancelText="No"
                  >
                    <Button 
                      size="small" 
                      danger 
                      icon={<DeleteOutlined />}
                    />
                  </Popconfirm>
                </div>
              ))}
            </Space>
          ) : (
            <p className="text-gray-400">No options yet</p>
          )}
        </div>
      </Modal>

      {/* Assign Task Modal */}
      <Modal
        title="Assign Task to Staff"
        open={assignTaskModalVisible}
        onCancel={() => {
          setAssignTaskModalVisible(false);
          assignForm.resetFields();
        }}
        onOk={() => assignForm.submit()}
      >
        <Form form={assignForm} onFinish={handleAssignTask} layout="vertical">
          <Form.Item
            name="taskId"
            label="Task"
            rules={[{ required: true, message: 'Please select task' }]}
          >
            <Select 
              placeholder="Select task"
              onChange={(taskId) => {
                handleTaskSelect(taskId);
                assignForm.setFieldsValue({ optionId: undefined }); // Reset option when task changes
              }}
            >
              {tasks.map(task => (
                <Select.Option key={task.id} value={task.id}>
                  {task.name}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="optionId"
            label="Task Option"
            rules={[{ required: true, message: 'Please select task option' }]}
          >
            <Select 
              placeholder={selectedTask ? "Select task option" : "Please select a task first"}
              disabled={!selectedTask || !taskOptions.length}
            >
              {taskOptions.map(option => (
                <Select.Option key={option.id} value={option.id}>
                  {option.optionName}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="staffId"
            label="Staff"
            rules={[{ required: true, message: 'Please select staff' }]}
          >
            <Select placeholder="Select staff">
              {staffs.map(staff => (
                <Select.Option key={staff.id} value={staff.id}>
                  {staff.userName || staff.userEmail}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default StaffManagement;
